// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [CustomPropertyDrawer(typeof(SearchContextAttribute))]
    sealed class SearchContextPropertyDrawer : PropertyDrawer
    {
        static readonly string kVisualElementName = "SearchContext";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUI.GetSinglePropertyHeight(property, label);
            var searchContextAttribute = (SearchContextAttribute)attribute;
            var searchContextFlags = searchContextAttribute.flags;
            GetSearchContextDataFromProperty(property, searchContextAttribute, out var searchContext, out var objType);
            ObjectField.DoObjectField(position, property, objType, label, searchContext, searchContextFlags);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty prop)
        {
            var searchContextAttribute = (SearchContextAttribute)attribute;
            GetSearchContextDataFromProperty(prop, searchContextAttribute, out var searchContext, out var objectType);
            ObjectField obj = new ObjectField()
            {
                name = kVisualElementName,
                label = preferredLabel,
                bindingPath = prop.propertyPath,
                objectType = objectType,
                searchViewFlags = searchContextAttribute.flags,
                searchContext = searchContext
            };
            obj.AddToClassList(ObjectField.alignedFieldUssClassName);

            return obj;
        }

        internal static void GetSearchContextDataFromProperty(SerializedProperty property, SearchContextAttribute searchContextAttribute, out SearchContext searchContext, out Type objectType)
        {
            objectType = GetPropertyType(property);
            searchContext = CreateContextFromAttribute(searchContextAttribute);
            var runtimeSearchContext = CreateRuntimeSearchContextFromAttribute(property, objectType);
            searchContext.runtimeContext = runtimeSearchContext;
        }

        internal static RuntimeSearchContext CreateRuntimeSearchContextFromAttribute(SerializedProperty property, Type objType)
        {
            return new RuntimeSearchContext
            {
                contextId = property.propertyPath,
                pickerType = SearchPickerType.SearchContextAttribute,
                currentObject = property.objectReferenceValue,
                editedObjects = property.serializedObject.targetObjects,
                requiredTypes = new Type[] { objType },
                requiredTypeNames = new[] { objType.ToString() }
            };
        }

        internal static SearchContext CreateContextFromAttribute(SearchContextAttribute attribute)
        {
            var providers = attribute.providerIds.Select(id => SearchService.GetProvider(id))
                .Concat(attribute.instantiableProviders.Select(type => SearchService.GetProvider(type))).Where(p => p != null);

            if (!providers.Any())
                providers = SearchService.GetObjectProviders();

            var searchText = attribute.query;
            var searchQuery = GetSearchQueryFromFromAttribute(attribute);
            if (searchQuery != null)
            {
                searchText = searchQuery.text;
                providers = SearchUtils.GetMergedProviders(providers, searchQuery.providerIds);
            }

            var context = SearchService.CreateContext(providers, searchText, SearchFlags.Default | SearchFlags.UseSessionSettings);
            return context;
        }

        static SearchQueryAsset GetSearchQueryFromFromAttribute(SearchContextAttribute attribute)
        {
            return GetSearchQueryFromFromAttribute(attribute.query);
        }

        internal static SearchQueryAsset GetSearchQueryFromFromAttribute(string attributeQuery)
        {
            var pathOrGuid = attributeQuery.Trim();

            // Check if it's a path that exists
            if (File.Exists(pathOrGuid))
                return AssetDatabase.LoadAssetAtPath<SearchQueryAsset>(pathOrGuid);

            // Is it a GUID?
            if (GUID.TryParse(pathOrGuid, out var guid))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    return null;
                if (!File.Exists(assetPath))
                    return null;
                return AssetDatabase.LoadAssetAtPath<SearchQueryAsset>(assetPath);
            }

            return null;
        }

        static Type GetPropertyType(SerializedProperty property)
        {
            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out var type);
            return type;
        }
    }
}
