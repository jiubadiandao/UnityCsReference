// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal
{
    internal class ProfilerChart : Chart
    {
        public const int k_MaximumSeriesCount = 10;
        public const float k_ChartMinClamp = 110.0f;
        public const float k_ChartMaxClamp = 70000.0f;

        const string k_ProfilerChartSettingsPreferenceKeyFormat = "ProfilerChart.{0}.ChartSettings";

        public ProfilerArea m_Area;
        public ProfilerModuleChartType m_Type;
        public float m_DataScale;
        public ChartViewData m_Data;
        public ChartSeriesViewData[] m_Series;
        // For some charts, every line is scaled individually, so every data series gets their own range based on their own max scale.
        // For charts that share their scale (like the Networking charts) all series get adjusted to the total max of the chart.
        // Shared scale is only used for line charts and should only be used when every series shares the same data unit.
        public bool m_SharedScale;

        string m_Name;
        string m_LocalizedName;
        string m_IconName;
        float m_MaximumScaleInterpolationValue;

        public bool ShowGrid { get; set; }
        public string Tooltip { get; set; }

        public ProfilerChart(ProfilerArea area, ProfilerModuleChartType type, float dataScale, float maximumScaleInterpolationValue, int seriesCount, string name, string localizedName, string iconName)
        {
            Debug.Assert(seriesCount <= k_MaximumSeriesCount);

            labelRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            graphRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            m_Area = area;
            m_Type = type;
            m_DataScale = dataScale;
            m_MaximumScaleInterpolationValue = maximumScaleInterpolationValue;
            m_Data = new ChartViewData();
            m_Series = new ChartSeriesViewData[seriesCount];
            m_Name = name;
            m_LocalizedName = localizedName;
            m_IconName = iconName;
            ShowGrid = (type == ProfilerModuleChartType.StackedTimeArea);

            var localizedTooltipFormat = LocalizationDatabase.GetLocalizedString("A chart showing performance counters related to '{0}'.");
            Tooltip = string.Format(localizedTooltipFormat, m_LocalizedName);
        }

        string ChartSettingsPreferenceKey => string.Format(k_ProfilerChartSettingsPreferenceKeyFormat, m_Name);
        bool usesCounters => ((uint)m_Area == Profiler.invalidProfilerArea);

        /// <summary>
        /// Warning message to warn user about module pecular properties, such as extra overhead or limited availability
        /// </summary>
        public GUIContent WarningMsg { get; set; }

        /// <summary>
        /// Callback parameter will be either true if a state change occured
        /// </summary>
        /// <param name="onSeriesToggle"></param>
        public void SetOnSeriesToggleCallback(Action<bool> onSeriesToggle)
        {
            onDoSeriesToggle = onSeriesToggle;
        }

        public void SetName(string name, string legacyPreferenceKey)
        {
            m_Name = name;

            var chartSettingsPreferenceKey = string.IsNullOrEmpty(legacyPreferenceKey) ? ChartSettingsPreferenceKey : legacyPreferenceKey;
            SetChartSettingsNameAndUpdateAllPreferences(chartSettingsPreferenceKey);
        }

        protected override void DoLegendGUI(Rect position, ProfilerModuleChartType type, ChartViewData cdata, EventType evtType, bool active)
        {
            base.DoLegendGUI(position, type, cdata, evtType, active);

            if (WarningMsg != null)
            {
                const float rightMmargin = 2f;
                const float topMargin = 4f;
                const float iconSize = 16f;
                var padding = GUISkin.current.label.padding;
                float width = iconSize + padding.horizontal;

                GUI.Label(new Rect(position.xMax - width - rightMmargin, position.y + topMargin, width, iconSize + padding.vertical), WarningMsg);
            }
        }

        public float GetMinimumHeight()
        {
            var seriesCount = m_Data.numSeries;
            var requiredHeight = k_LegendSeriesLabelOffset + ((seriesCount + 1) * k_LegendSeriesLabelHeight) + k_DistanceFromTopToFirstLegendLabel;
            return Math.Max(requiredHeight, k_MinimumHeight);
        }

        public virtual int DoChartGUI(Rect chartRect, int currentFrame, bool active)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string[] labels = new string[m_Series.Length];
                for (int s = 0; s < m_Series.Length; s++)
                {
                    string name =
                        m_Data.hasOverlay ?
                        "Selected" + m_Series[s].name :
                        m_Series[s].name;
                    if (usesCounters)
                    {
                        labels[s] = ProfilerDriver.GetFormattedCounterValue(currentFrame, m_Series[s].category, name);
                    }
                    else
                    {
                        labels[s] = ProfilerDriver.GetFormattedCounterValue(currentFrame, m_Area, name);
                    }
                }
                m_Data.AssignSelectedLabels(labels);
            }

            if (legendHeaderLabel == null)
            {
                legendHeaderLabel = new GUIContent(m_LocalizedName, EditorGUIUtility.LoadIconRequired(m_IconName), Tooltip);
            }

            return DoGUI(chartRect, m_Type, currentFrame, m_Data, active);
        }

        public void LoadAndBindSettings(string legacyPreferenceKey)
        {
            // Use the legacy preference key to maintain user settings on built-in modules.
            var chartSettingsPreferenceKey = string.IsNullOrEmpty(legacyPreferenceKey) ? ChartSettingsPreferenceKey : legacyPreferenceKey;
            LoadAndBindSettings(chartSettingsPreferenceKey, m_Data);
        }

        public virtual void UpdateData(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            // Clear the data available buffer prior to iterating over chart series.
            if (m_Series.Length > 0)
                m_Data.ClearDataAvailableBuffer();

            // Iterate over all chart series pulling counter data.
            float totalMaxValue = 1;
            for (int i = 0, count = m_Series.Length; i < count; ++i)
            {
                float maxValue;
                if (usesCounters)
                {
                    // Retrieve counter values by category name & counter name.
                    ProfilerDriver.GetCounterValuesWithAvailabilityBatchByCategory(m_Series[i].category, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, m_Data.dataAvailable, out maxValue);
                }
                else
                {
                    // Retrieve counter values by area & counter name.
                    ProfilerDriver.GetCounterValuesWithAvailabilityBatch(m_Area, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, m_Data.dataAvailable, out maxValue);
                }

                m_Series[i].yScale = m_DataScale;
                maxValue *= m_DataScale;

                // Minimum size so we don't generate nans during drawing
                maxValue = Mathf.Max(maxValue, 0.0001F);

                if (maxValue > totalMaxValue)
                    totalMaxValue = maxValue;

                if (m_Type == ProfilerModuleChartType.Line)
                {
                    // Scale line charts so they never hit the top. Scale them slightly differently for each line
                    // so that in "no stuff changing" case they will not end up being exactly the same.
                    maxValue *= (1.05f + i * 0.05f);
                    m_Series[i].rangeAxis = new Vector2(0f, maxValue);
                }
            }

            if (m_SharedScale && m_Type == ProfilerModuleChartType.Line)
            {
                // For some charts, every line is scaled individually, so every data series gets their own range based on their own max scale.
                // For charts that share their scale (like the Networking charts) all series get adjusted to the total max of the chart.
                for (int i = 0, count = m_Series.Length; i < count; ++i)
                    m_Series[i].rangeAxis = new Vector2(0f, (1.05f + i * 0.05f) * totalMaxValue);
                m_Data.maxValue = totalMaxValue;
            }
            m_Data.Assign(m_Series, firstEmptyFrame, firstFrame);
        }

        public void UpdateOverlayData(int firstEmptyFrame)
        {
            m_Data.hasOverlay = true;
            int numCharts = m_Data.numSeries;
            for (int i = 0; i < numCharts; ++i)
            {
                var chart = m_Data.series[i];
                var length = chart.yValues.Length;
                if (m_Data.overlays[i] == null || m_Data.overlays[i].yValues.Length != length)
                {
                    m_Data.overlays[i] = new ChartSeriesViewData(chart.name, chart.category, length, chart.color);
                }
                float maxValue;
                ProfilerDriver.GetCounterValuesBatch(ProfilerArea.CPU, string.Format("Selected{0}", chart.name), firstEmptyFrame, 1.0f, m_Data.overlays[i].yValues, out maxValue);
                m_Data.overlays[i].yScale = m_DataScale;
            }
        }

        public void UpdateScaleValuesIfNecessary(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            if (m_Type.IsStackedChartType())
            {
                ComputeChartScaleValue(firstEmptyFrame, firstFrame, frameCount);
            }
        }

        public void ComputeChartScaleValue(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            float timeMax = 0.0f;
            float timeMaxExcludeFirst = 0.0f;
            // TODO: optimze this. it's not terrible but not cache friendly either and the yValues are the once needed to be accessed more often than the series
            // This takes up som 1 ms in deep profiling with 300 frames and likely scales badly with more
            for (int k = 0; k < frameCount; k++)
            {
                float timeNow = 0.0F;
                for (int j = 0; j < m_Series.Length; j++)
                {
                    var series = m_Series[j];

                    if (series.enabled)
                        timeNow += series.yValues[k];
                }
                if (timeNow > timeMax)
                    timeMax = timeNow;
                if (timeNow > timeMaxExcludeFirst && k + firstEmptyFrame >= firstFrame + 1)
                    timeMaxExcludeFirst = timeNow;
            }
            if (timeMaxExcludeFirst != 0.0f)
                timeMax = timeMaxExcludeFirst;

            if (m_Type == ProfilerModuleChartType.StackedTimeArea)
                timeMax = Mathf.Clamp(timeMax * m_DataScale, k_ChartMinClamp, k_ChartMaxClamp);

            // Do not apply the new scale immediately, but gradually go towards it
            if (m_MaximumScaleInterpolationValue > 0.0f)
                timeMax = Mathf.Lerp(m_MaximumScaleInterpolationValue, timeMax, 0.4f);
            m_MaximumScaleInterpolationValue = timeMax;

            for (int k = 0; k < m_Data.numSeries; ++k)
                m_Data.series[k].rangeAxis = new Vector2(0f, timeMax);
            m_Data.UpdateChartGrid(timeMax, ShowGrid);
        }

        public void ConfigureChartSeries(int historySize, ProfilerCounterDescriptor[] counters)
        {
            var chartAreaColors = ProfilerColors.chartAreaColors;
            for (int i = 0; i < counters.Length; ++i)
            {
                var counter = counters[i];
                var category = counter.CategoryName;
                m_Series[i] = new ChartSeriesViewData(counter.Name, category, historySize, chartAreaColors[i % chartAreaColors.Length]);
            }

            // Allocate dataAvailable array for chart.
            m_Data.dataAvailable = new int[historySize];
        }

        public void ResetChartState()
        {
            m_MaximumScaleInterpolationValue = 0;
        }
    }
}
