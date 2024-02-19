// -----------------------------------------------------------------------
// <copyright file="UnityFuncDrawer.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.SerializableFunc.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using Object = UnityEngine.Object;
    using PersistentListenerMode = UnityEngine.Events.PersistentListenerMode;
    using UnityEventCallState = UnityEngine.Events.UnityEventCallState;

    [CustomPropertyDrawer(typeof(UnityFuncBase), true)]
    internal class UnityFuncDrawer : PropertyDrawer
    {
        private const int kExtraSpacing = 9;
        private const float kSpacing = 5;
        private static readonly GUIContent mixedValueContent = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");

        private static readonly string kNoFunctionString = "No Function";

        // PersistentCall Paths
        private static readonly string kCallStatePath = "m_CallState";
        private static readonly string kArgumentsPath = "m_Arguments";
        private static readonly string kMethodNamePath = "m_MethodName";
        private static readonly string kModePath = "m_Mode";
        private static readonly string kTargetPath = "m_Target";

        // ArgumentCache paths
        private static readonly string kFloatArgument = "m_FloatArgument";
        private static readonly string kIntArgument = "m_IntArgument";
        private static readonly string kObjectArgument = "m_ObjectArgument";
        private static readonly string kStringArgument = "m_StringArgument";
        private static readonly string kBoolArgument = "m_BoolArgument";
        private static readonly string kObjectArgumentAssemblyTypeName = "m_ObjectArgumentAssemblyTypeName";

        // UnityFuncBase/PersistentCallGroup paths
        private static readonly string kPersistentCallsPath = "persistentCalls.calls";

        // property path splits and separators
        private static readonly string kDotString = ".";
        private static readonly string kArrayDataString = "Array.data[";
        private static readonly char[] kDotSeparator = { '.' };
        private static readonly char[] kClosingSquareBraceSeparator = { ']' };

        private string labelText;
        private UnityFuncBase dummyEvent;
        private SerializedProperty prop;
        private SerializedProperty listenersArray;

        // State:
        private ReorderableList reorderableList;
        private int lastSelectedIndex;
        private Dictionary<string, State> states = new Dictionary<string, State>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            this.prop = property;
            this.labelText = label.text;

            State state = this.RestoreState(property);

            this.OnGUI(position);
            state.lastSelectedIndex = this.lastSelectedIndex;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // TODO: Also we need to have a constructor or initializer called for this property Drawer, before OnGUI or GetPropertyHeight
            // otherwise, we get Restore the State twice, once here and again in OnGUI. Maybe we should only do it here?
            this.RestoreState(property);

            var height = 0f;
            if (this.reorderableList != null)
            {
                height = this.reorderableList.GetHeight();
            }

            return height;
        }

        public void OnGUI(Rect position)
        {
            if (this.listenersArray == null || !this.listenersArray.isArray)
            {
                return;
            }

            this.dummyEvent = GetDummyEvent(this.prop);
            if (this.dummyEvent == null)
            {
                return;
            }

            if (this.reorderableList != null)
            {
                var oldIdentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                this.reorderableList.DoList(position);
                EditorGUI.indentLevel = oldIdentLevel;
            }
        }

        protected virtual void SetupReorderableList(ReorderableList list)
        {
            // Two standard lines with standard spacing between and extra spacing below to better separate items visually.
            list.elementHeight = (EditorGUIUtility.singleLineHeight * 2) + EditorGUIUtility.standardVerticalSpacing + kExtraSpacing;
        }

        protected virtual void DrawEventHeader(Rect headerRect)
        {
            headerRect.height = EditorGUIUtility.singleLineHeight;
            var text = (string.IsNullOrEmpty(this.labelText) ? "Event" : this.labelText) + GetEventParams(this.dummyEvent);
            GUI.Label(headerRect, text);
        }

        private static PersistentListenerMode GetMode(SerializedProperty mode)
        {
            return (PersistentListenerMode)mode.enumValueIndex;
        }

        private static string GetEventParams(UnityFuncBase evt)
        {
            var methodInfo = evt.FindMethod("Invoke", evt, PersistentListenerMode.EventDefined, null);

            var sb = new StringBuilder();
            sb.Append(" (");

            var types = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            for (var i = 0; i < types.Length; i++)
            {
                sb.Append(types[i].Name);
                if (i < types.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(")");
            return sb.ToString();
        }

        private static UnityFuncBase GetDummyEvent(SerializedProperty prop)
        {
            // Use the SerializedProperty path to iterate through the fields of the inspected targetObject
            Object tgtobj = prop.serializedObject.targetObject;
            if (tgtobj == null)
            {
                return new UnityFunc<object>();
            }

            UnityFuncBase ret = null;
            Type ft = tgtobj.GetType();
            var bindflags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            do
            {
                ret = GetDummyEventHelper(prop.propertyPath, ft, bindflags);

                // no need to look for public members again since the base type covered that
                bindflags = BindingFlags.Instance | BindingFlags.NonPublic;
                ft = ft.BaseType;
            }
            while (ret == null && ft != null);

            // go up the class hierarchy if it exists and the property is not found on the child
            return (ret == null) ? new UnityFunc<object>() : ret;
        }

        private static UnityFuncBase GetDummyEventHelper(string propPath, Type targetObjectType, BindingFlags flags)
        {
            if (targetObjectType == null)
            {
                return null;
            }

            while (propPath.Length != 0)
            {
                // we could have a leftover '.' if the previous iteration handled an array element
                if (propPath.StartsWith(kDotString, StringComparison.Ordinal))
                {
                    propPath = propPath.Substring(1);
                }

                var splits = propPath.Split(kDotSeparator, 2);
                var newField = targetObjectType.GetField(splits[0], flags);
                if (newField == null)
                {
                    return GetDummyEventHelper(propPath, targetObjectType.BaseType, flags);
                }

                targetObjectType = newField.FieldType;
                if (IsArrayOrList(targetObjectType))
                {
                    targetObjectType = GetArrayOrListElementType(targetObjectType);
                }

                // the last item in the property path could have been an array element
                // bail early in that case
                if (splits.Length == 1)
                {
                    break;
                }

                propPath = splits[1];
                if (propPath.StartsWith(kArrayDataString, StringComparison.Ordinal))
                {
                    propPath = propPath.Split(kClosingSquareBraceSeparator, 2)[1];
                }
            }

            if (targetObjectType.IsSubclassOf(typeof(UnityFuncBase)))
            {
                return Activator.CreateInstance(targetObjectType) as UnityFuncBase;
            }

            return null;
        }

        private static bool IsArrayOrList(Type listType)
        {
            if (listType.IsArray)
            {
                return true;
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }

            return false;
        }

        private static Type GetArrayOrListElementType(Type listType)
        {
            if (listType.IsArray)
            {
                return listType.GetElementType();
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return listType.GetGenericArguments()[0];
            }

            return null;
        }

        private static IEnumerable<ValidMethodMap> CalculateMethodMap(Object target, Type[] t, Type delegateReturnType, bool allowSubclasses)
        {
            var validMethods = new List<ValidMethodMap>();
            if (target == null || t == null)
            {
                return validMethods;
            }

            // find the methods on the behaviour that match the signature
            Type componentType = target.GetType();
            var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

            var wantedProperties = componentType.GetProperties().AsEnumerable();
            wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetGetMethod() != null);
            componentMethods.AddRange(wantedProperties.Select(x => x.GetGetMethod()));

            foreach (var componentMethod in componentMethods)
            {
                // Debug.Log ("Method: " + componentMethod);
                // if the argument length is not the same, no match
                var componentParamaters = componentMethod.GetParameters();
                if (componentParamaters.Length != t.Length)
                {
                    continue;
                }

                // Don't show obsolete methods.
                if (componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                {
                    continue;
                }

                if (componentMethod.ReturnType != delegateReturnType)
                {
                    continue;
                }

                // if the argument types do not match, no match
                var paramatersMatch = true;
                for (var i = 0; i < t.Length; i++)
                {
                    if (!componentParamaters[i].ParameterType.IsAssignableFrom(t[i]))
                    {
                        paramatersMatch = false;
                    }

                    if (allowSubclasses && t[i].IsAssignableFrom(componentParamaters[i].ParameterType))
                    {
                        paramatersMatch = true;
                    }
                }

                // valid method
                if (paramatersMatch)
                {
                    var vmm = new ValidMethodMap
                    {
                        target = target,
                        methodInfo = componentMethod,
                    };
                    validMethods.Add(vmm);
                }
            }

            return validMethods;
        }

        private static bool IsPersistantListenerValid(UnityFuncBase dummyEvent, string methodName, Object uObject, PersistentListenerMode modeEnum, Type argumentType)
        {
            if (uObject == null || string.IsNullOrEmpty(methodName))
            {
                return false;
            }

            return dummyEvent.FindMethod(methodName, uObject, modeEnum, argumentType) != null;
        }

        private static GenericMenu BuildPopupList(Object target, UnityFuncBase dummyEvent, SerializedProperty listener)
        {
            // special case for components... we want all the game objects targets there!
            var targetToUse = target;
            if (targetToUse is Component)
            {
                targetToUse = (target as Component).gameObject;
            }

            // find the current event target...
            var methodName = listener.FindPropertyRelative(kMethodNamePath);

            var menu = new GenericMenu();
            menu.AddItem(
                new GUIContent(kNoFunctionString),
                string.IsNullOrEmpty(methodName.stringValue),
                ClearEventFunction,
                new UnityEventFunction(listener, null, null, PersistentListenerMode.EventDefined));

            if (targetToUse == null)
            {
                return menu;
            }

            menu.AddSeparator(string.Empty);

            // figure out the signature of this delegate...
            // The property at this stage points to the 'container' and has the field name
            Type delegateType = dummyEvent.GetType();

            // check out the signature of invoke as this is the callback!
            MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
            var delegateArgumentsTypes = delegateMethod.GetParameters().Select(x => x.ParameterType).ToArray();
            var delegateReturnType = delegateMethod.ReturnType;

            GeneratePopUpForType(menu, targetToUse, false, listener, delegateArgumentsTypes, delegateReturnType);
            if (targetToUse is GameObject)
            {
                Component[] comps = (targetToUse as GameObject).GetComponents<Component>();
                var duplicateNames = comps.Where(c => c != null).Select(c => c.GetType().Name).GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                foreach (Component comp in comps)
                {
                    if (comp == null)
                    {
                        continue;
                    }

                    GeneratePopUpForType(menu, comp, duplicateNames.Contains(comp.GetType().Name), listener, delegateArgumentsTypes, delegateReturnType);
                }
            }

            return menu;
        }

        private static void GeneratePopUpForType(GenericMenu menu, Object target, bool useFullTargetName, SerializedProperty listener, Type[] delegateArgumentsTypes, Type delegateReturnType)
        {
            var methods = new List<ValidMethodMap>();
            var targetName = useFullTargetName ? target.GetType().FullName : target.GetType().Name;

            var didAddDynamic = false;

            // skip 'void' event defined on the GUI as we have a void prebuilt type!
            if (delegateArgumentsTypes.Length != 0)
            {
                GetMethodsForTargetAndMode(target, delegateArgumentsTypes, delegateReturnType, methods, PersistentListenerMode.EventDefined);
                if (methods.Count > 0)
                {
                    menu.AddDisabledItem(new GUIContent(targetName + "/Dynamic " + string.Join(", ", delegateArgumentsTypes.Select(e => GetTypeName(e)).ToArray())));
                    AddMethodsToMenu(menu, listener, methods, targetName);
                    didAddDynamic = true;
                }
            }

            methods.Clear();
            GetMethodsForTargetAndMode(target, new[] { typeof(float) }, delegateReturnType, methods, PersistentListenerMode.Float);
            GetMethodsForTargetAndMode(target, new[] { typeof(int) }, delegateReturnType, methods, PersistentListenerMode.Int);
            GetMethodsForTargetAndMode(target, new[] { typeof(string) }, delegateReturnType, methods, PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(bool) }, delegateReturnType, methods, PersistentListenerMode.Bool);
            GetMethodsForTargetAndMode(target, new[] { typeof(Object) }, delegateReturnType, methods, PersistentListenerMode.Object);
            GetMethodsForTargetAndMode(target, Array.Empty<Type>(), delegateReturnType, methods, PersistentListenerMode.Void);
            if (methods.Count > 0)
            {
                if (didAddDynamic)
                {
                    // AddSeperator doesn't seem to work for sub-menus, so we have to use this workaround instead of a proper separator for now.
                    menu.AddItem(new GUIContent(targetName + "/ "), false, null);
                }

                if (delegateArgumentsTypes.Length != 0)
                {
                    menu.AddDisabledItem(new GUIContent(targetName + "/Static Parameters"));
                }

                AddMethodsToMenu(menu, listener, methods, targetName);
            }
        }

        private static void AddMethodsToMenu(GenericMenu menu, SerializedProperty listener, List<ValidMethodMap> methods, string targetName)
        {
            // Note: sorting by a bool in OrderBy doesn't seem to work for some reason, so using numbers explicitly.
            IEnumerable<ValidMethodMap> orderedMethods = methods.OrderBy(e => e.methodInfo.Name.StartsWith("set_", StringComparison.Ordinal) ? 0 : 1).ThenBy(e => e.methodInfo.Name);
            foreach (var validMethod in orderedMethods)
            {
                AddFunctionsForScript(menu, listener, validMethod, targetName);
            }
        }

        private static void GetMethodsForTargetAndMode(Object target, Type[] delegateArgumentsTypes, Type delegateReturnType, List<ValidMethodMap> methods, PersistentListenerMode mode)
        {
            IEnumerable<ValidMethodMap> newMethods = CalculateMethodMap(target, delegateArgumentsTypes, delegateReturnType, mode == PersistentListenerMode.Object);
            foreach (var m in newMethods)
            {
                var method = m;
                method.mode = mode;
                methods.Add(method);
            }
        }

        private static void AddFunctionsForScript(GenericMenu menu, SerializedProperty listener, ValidMethodMap method, string targetName)
        {
            PersistentListenerMode mode = method.mode;

            // find the current event target...
            var listenerTarget = listener.FindPropertyRelative(kTargetPath).objectReferenceValue;
            var methodName = listener.FindPropertyRelative(kMethodNamePath).stringValue;
            var setMode = GetMode(listener.FindPropertyRelative(kModePath));
            var typeName = listener.FindPropertyRelative(kArgumentsPath).FindPropertyRelative(kObjectArgumentAssemblyTypeName);

            var args = new StringBuilder();
            var count = method.methodInfo.GetParameters().Length;
            for (var index = 0; index < count; index++)
            {
                var methodArg = method.methodInfo.GetParameters()[index];
                args.Append($"{GetTypeName(methodArg.ParameterType)}");

                if (index < count - 1)
                {
                    args.Append(", ");
                }
            }

            var isCurrentlySet = listenerTarget == method.target
                && methodName == method.methodInfo.Name
                && mode == setMode;

            if (isCurrentlySet && mode == PersistentListenerMode.Object && method.methodInfo.GetParameters().Length == 1)
            {
                isCurrentlySet &= method.methodInfo.GetParameters()[0].ParameterType.AssemblyQualifiedName == typeName.stringValue;
            }

            var path = GetFormattedMethodName(targetName, method.methodInfo.Name, args.ToString(), mode == PersistentListenerMode.EventDefined);
            menu.AddItem(
                new GUIContent(path),
                isCurrentlySet,
                SetEventFunction,
                new UnityEventFunction(listener, method.target, method.methodInfo, mode));
        }

        private static string GetTypeName(Type t)
        {
            if (t == typeof(int))
            {
                return "int";
            }

            if (t == typeof(float))
            {
                return "float";
            }

            if (t == typeof(string))
            {
                return "string";
            }

            if (t == typeof(bool))
            {
                return "bool";
            }

            return t.Name;
        }

        private static string GetFormattedMethodName(string targetName, string methodName, string args, bool dynamic)
        {
            if (dynamic)
            {
                if (methodName.StartsWith("set_", StringComparison.Ordinal))
                {
                    // return string.Format("{0}/{1}", targetName, methodName.Substring(4));
                    return $"{targetName}/{methodName.Substring(4)}";
                }
                else
                {
                    // return string.Format("{0}/{1}", targetName, methodName);
                    return $"{targetName}/{methodName}";
                }
            }
            else
            {
                if (methodName.StartsWith("set_", StringComparison.Ordinal))
                {
                    // return string.Format("{0}/{2} {1}", targetName, methodName.Substring(4), args);
                    return $"{targetName}/{args} {methodName.Substring(4)}";
                }
                else
                {
                    // return string.Format("{0}/{1} ({2})", targetName, methodName, args);
                    return $"{targetName}/{methodName} ({args})";
                }
            }
        }

        private static void SetEventFunction(object source)
        {
            ((UnityEventFunction)source).Assign();
        }

        private static void ClearEventFunction(object source)
        {
            ((UnityEventFunction)source).Clear();
        }

        private State GetState(SerializedProperty prop)
        {
            var key = prop.propertyPath;
            this.states.TryGetValue(key, out State state);

            // ensure the cached SerializedProperty is synchronized (case 974069)
            if (state == null || state.reorderableList.serializedProperty.serializedObject != prop.serializedObject)
            {
                if (state == null)
                {
                    state = new State();
                }

                SerializedProperty listenersArray = prop.FindPropertyRelative(kPersistentCallsPath);
                state.reorderableList =
                    new ReorderableList(prop.serializedObject, listenersArray, false, true, true, true)
                    {
                        drawHeaderCallback = this.DrawEventHeader,
                        drawElementCallback = this.DrawEvent,
                        onSelectCallback = this.OnSelectEvent,
                        onReorderCallback = this.OnReorderEvent,
                        onAddCallback = this.OnAddEvent,
                        onRemoveCallback = this.OnRemoveEvent,
                    };
                this.SetupReorderableList(state.reorderableList);

                this.states[key] = state;
            }

            return state;
        }

        private State RestoreState(SerializedProperty property)
        {
            State state = this.GetState(property);

            this.listenersArray = state.reorderableList.serializedProperty;
            this.reorderableList = state.reorderableList;
            this.lastSelectedIndex = state.lastSelectedIndex;
            this.reorderableList.index = this.lastSelectedIndex;

            return state;
        }

        private void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
        {
            var pListener = this.listenersArray.GetArrayElementAtIndex(index);

            rect.y++;
            Rect[] subRects = this.GetRowRects(rect);
            Rect enabledRect = subRects[0];
            Rect goRect = subRects[1];
            Rect functionRect = subRects[2];
            Rect argRect = subRects[3];

            // find the current event target...
            var callState = pListener.FindPropertyRelative(kCallStatePath);
            var mode = pListener.FindPropertyRelative(kModePath);
            var arguments = pListener.FindPropertyRelative(kArgumentsPath);
            var listenerTarget = pListener.FindPropertyRelative(kTargetPath);
            var methodName = pListener.FindPropertyRelative(kMethodNamePath);

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            EditorGUI.PropertyField(enabledRect, callState, GUIContent.none);

            EditorGUI.BeginChangeCheck();
            {
                GUI.Box(goRect, GUIContent.none);
                EditorGUI.PropertyField(goRect, listenerTarget, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    methodName.stringValue = null;
                }
            }

            SerializedProperty argument;
            var modeEnum = GetMode(mode);

            // only allow argument if we have a valid target / method
            if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
            {
                modeEnum = PersistentListenerMode.Void;
            }

            switch (modeEnum)
            {
                case PersistentListenerMode.Float:
                    argument = arguments.FindPropertyRelative(kFloatArgument);
                    break;
                case PersistentListenerMode.Int:
                    argument = arguments.FindPropertyRelative(kIntArgument);
                    break;
                case PersistentListenerMode.Object:
                    argument = arguments.FindPropertyRelative(kObjectArgument);
                    break;
                case PersistentListenerMode.String:
                    argument = arguments.FindPropertyRelative(kStringArgument);
                    break;
                case PersistentListenerMode.Bool:
                    argument = arguments.FindPropertyRelative(kBoolArgument);
                    break;
                default:
                    argument = arguments.FindPropertyRelative(kIntArgument);
                    break;
            }

            var desiredArgTypeName = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue;
            var desiredType = typeof(Object);
            if (!string.IsNullOrEmpty(desiredArgTypeName))
            {
                desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);
            }

            if (modeEnum == PersistentListenerMode.Object)
            {
                EditorGUI.BeginChangeCheck();
                var result = EditorGUI.ObjectField(argRect, GUIContent.none, argument.objectReferenceValue, desiredType, true);
                if (EditorGUI.EndChangeCheck())
                {
                    argument.objectReferenceValue = result;
                }
            }
            else if (modeEnum != PersistentListenerMode.Void && modeEnum != PersistentListenerMode.EventDefined)
            {
                EditorGUI.PropertyField(argRect, argument, GUIContent.none);
            }

            using (new EditorGUI.DisabledScope(listenerTarget.objectReferenceValue == null))
            {
                EditorGUI.BeginProperty(functionRect, GUIContent.none, methodName);
                {
                    GUIContent buttonContent;
                    if (EditorGUI.showMixedValue)
                    {
                        buttonContent = mixedValueContent;
                    }
                    else
                    {
                        var buttonLabel = new StringBuilder();
                        if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
                        {
                            buttonLabel.Append(kNoFunctionString);
                        }
                        else if (!IsPersistantListenerValid(this.dummyEvent, methodName.stringValue, listenerTarget.objectReferenceValue, GetMode(mode), desiredType))
                        {
                            var instanceString = "UnknownComponent";
                            var instance = listenerTarget.objectReferenceValue;
                            if (instance != null)
                            {
                                instanceString = instance.GetType().Name;
                            }

                            // buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName.stringValue));
                            buttonLabel.Append($"<Missing {instanceString}.{methodName.stringValue}>");
                        }
                        else
                        {
                            buttonLabel.Append(listenerTarget.objectReferenceValue.GetType().Name);

                            if (!string.IsNullOrEmpty(methodName.stringValue))
                            {
                                buttonLabel.Append(".");
                                if (methodName.stringValue.StartsWith("set_", StringComparison.Ordinal))
                                {
                                    buttonLabel.Append(methodName.stringValue.Substring(4));
                                }
                                else
                                {
                                    buttonLabel.Append(methodName.stringValue);
                                }
                            }
                        }

                        buttonContent = new GUIContent(buttonLabel.ToString());
                    }

                    if (GUI.Button(functionRect, buttonContent, EditorStyles.popup))
                    {
                        BuildPopupList(listenerTarget.objectReferenceValue, this.dummyEvent, pListener).DropDown(functionRect);
                    }
                }

                EditorGUI.EndProperty();
            }

            GUI.backgroundColor = c;
        }

        private Rect[] GetRowRects(Rect rect)
        {
            var rects = new Rect[4];

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2;

            Rect enabledRect = rect;
            enabledRect.width *= 0.3f;

            Rect goRect = enabledRect;
            goRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect functionRect = rect;
            functionRect.xMin = goRect.xMax + kSpacing;

            Rect argRect = functionRect;
            argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            rects[0] = enabledRect;
            rects[1] = goRect;
            rects[2] = functionRect;
            rects[3] = argRect;
            return rects;
        }

        private void OnRemoveEvent(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            this.lastSelectedIndex = list.index;
        }

        private void OnAddEvent(ReorderableList list)
        {
            if (this.listenersArray.hasMultipleDifferentValues)
            {
                // When increasing a multi-selection array using Serialized Property
                // Data can be overwritten if there is mixed values.
                // The Serialization system applies the Serialized data of one object, to all other objects in the selection.
                // We handle this case here, by creating a SerializedObject for each object.
                // Case 639025.
                foreach (var targetObject in this.listenersArray.serializedObject.targetObjects)
                {
                    var temSerialziedObject = new SerializedObject(targetObject);
                    var listenerArrayProperty = temSerialziedObject.FindProperty(this.listenersArray.propertyPath);
                    listenerArrayProperty.arraySize += 1;
                    temSerialziedObject.ApplyModifiedProperties();
                }

                this.listenersArray.serializedObject.SetIsDifferentCacheDirty();
                this.listenersArray.serializedObject.Update();
                list.index = list.serializedProperty.arraySize - 1;
            }
            else
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
            }

            this.lastSelectedIndex = list.index;
            var pListener = this.listenersArray.GetArrayElementAtIndex(list.index);

            var callState = pListener.FindPropertyRelative(kCallStatePath);
            var listenerTarget = pListener.FindPropertyRelative(kTargetPath);
            var methodName = pListener.FindPropertyRelative(kMethodNamePath);
            var mode = pListener.FindPropertyRelative(kModePath);
            var arguments = pListener.FindPropertyRelative(kArgumentsPath);

            callState.enumValueIndex = (int)UnityEventCallState.RuntimeOnly;
            listenerTarget.objectReferenceValue = null;
            methodName.stringValue = null;
            mode.enumValueIndex = (int)PersistentListenerMode.Void;
            arguments.FindPropertyRelative(kFloatArgument).floatValue = 0;
            arguments.FindPropertyRelative(kIntArgument).intValue = 0;
            arguments.FindPropertyRelative(kObjectArgument).objectReferenceValue = null;
            arguments.FindPropertyRelative(kStringArgument).stringValue = null;
            arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue = null;
        }

        private void OnSelectEvent(ReorderableList list)
        {
            this.lastSelectedIndex = list.index;
        }

        private void OnReorderEvent(ReorderableList list)
        {
            this.lastSelectedIndex = list.index;
        }

        internal readonly struct UnityEventFunction
        {
            internal readonly SerializedProperty listener;
            internal readonly Object target;
            internal readonly MethodInfo method;
            internal readonly PersistentListenerMode mode;

            public UnityEventFunction(SerializedProperty listener, Object target, MethodInfo method, PersistentListenerMode mode)
            {
                this.listener = listener;
                this.target = target;
                this.method = method;
                this.mode = mode;
            }

            public void Assign()
            {
                // find the current event target...
                var listenerTarget = this.listener.FindPropertyRelative(kTargetPath);
                var methodName = this.listener.FindPropertyRelative(kMethodNamePath);
                var mode = this.listener.FindPropertyRelative(kModePath);
                var arguments = this.listener.FindPropertyRelative(kArgumentsPath);

                listenerTarget.objectReferenceValue = this.target;
                methodName.stringValue = this.method.Name;
                mode.enumValueIndex = (int)this.mode;

                if (this.mode == PersistentListenerMode.Object)
                {
                    var fullArgumentType = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName);
                    var argParams = this.method.GetParameters();
                    if (argParams.Length == 1 && typeof(Object).IsAssignableFrom(argParams[0].ParameterType))
                    {
                        fullArgumentType.stringValue = argParams[0].ParameterType.AssemblyQualifiedName;
                    }
                    else
                    {
                        fullArgumentType.stringValue = typeof(Object).AssemblyQualifiedName;
                    }
                }

                this.ValidateObjectParamater(arguments, this.mode);

                this.listener.serializedObject.ApplyModifiedProperties();
            }

            public void Clear()
            {
                // find the current event target...
                var methodName = this.listener.FindPropertyRelative(kMethodNamePath);
                methodName.stringValue = null;

                var mode = this.listener.FindPropertyRelative(kModePath);
                mode.enumValueIndex = (int)PersistentListenerMode.Void;

                this.listener.serializedObject.ApplyModifiedProperties();
            }

            private void ValidateObjectParamater(SerializedProperty arguments, PersistentListenerMode mode)
            {
                var fullArgumentType = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName);
                var argument = arguments.FindPropertyRelative(kObjectArgument);
                var argumentObj = argument.objectReferenceValue;

                if (mode != PersistentListenerMode.Object)
                {
                    fullArgumentType.stringValue = typeof(Object).AssemblyQualifiedName;
                    argument.objectReferenceValue = null;
                    return;
                }

                if (argumentObj == null)
                {
                    return;
                }

                var t = Type.GetType(fullArgumentType.stringValue, false);
                if (!typeof(Object).IsAssignableFrom(t) || !t.IsInstanceOfType(argumentObj))
                {
                    argument.objectReferenceValue = null;
                }
            }
        }

        private struct ValidMethodMap
        {
            public Object target;
            public MethodInfo methodInfo;
            public PersistentListenerMode mode;
        }

        protected class State
        {
            public int lastSelectedIndex;
            internal ReorderableList reorderableList;
        }
    }
}
