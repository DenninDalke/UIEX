using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedOwl.Editor
{
    public static partial class RedOwlUtils
    {
        public static void Setup<T>(T instance, VisualElement element)
        {
            //Debug.LogFormat("Performing UIEX Setup on {0}", instance);
            Type type = instance.GetType();
            element.AddToClassList("RedOwl");
            element.AddToClassList(type.Namespace.Replace(".","_"));
            element.AddToClassList(type.Name);
            HandleUXMLAttribute(instance, element);
            HandleUSSAttributes(instance, element);
            HandleUSSClassAttributes(instance, element);
            HandleUXMLReferenceAttributes(instance, element);
            HandleQueryAttributes(instance, element);
            AddManipulators(instance, element);
            RegisterUICallbacks(instance, element);
        }
        
        public static string GetAssetNamespace<T>(T instance)
        {
            return instance.GetType().Namespace.Replace(".", "/");
        }
        
        public static string GetAssetName<T>(T instance)
        { 
            return instance.GetType().Name;
        }
        
        public static string GetUXMLPath<T>(T instance, string path)
        {
            return string.IsNullOrEmpty(path) ? string.Format("{0}/{1}Layout", GetAssetNamespace(instance), GetAssetName(instance)) : path;
        }
        
        public static string GetUSSPath<T>(T instance, string path)
        { 
            return string.IsNullOrEmpty(path) ? string.Format("{0}/{1}Style", GetAssetNamespace(instance), GetAssetName(instance)) : path;
        }
        
        public static void HandleUXMLAttribute<T>(T instance, VisualElement element)
        {
            instance.GetType().WithAttr<UXMLAttribute>((attr) => {
                string path = GetUXMLPath(instance, attr.path);
                var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (layout != null)
                {
                    //Debug.LogFormat("Loading '{0}.uxml' for '{1}'", path, instance.GetType().Name);
                    layout.CloneTree(element);
                }
            },
            false);
        }
        
        public static void HandleUSSAttributes<T>(T instance, VisualElement element)
        {
            instance.GetType().WithAttr<USSAttribute>((attr) => {
                string path = GetUSSPath(instance, attr.path);
                //Debug.LogFormat("Adding '{0}.uss' to '{1}'", path, instance.GetType().Name);
                element.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(path));
            },
            true);
        }
        
        public static void HandleUSSClassAttributes<T>(T instance, VisualElement element)
        {
            instance.GetType().WithAttr<USSClassAttribute>((attr) => {
                foreach (var name in attr.names)
                {
                    //Debug.LogFormat("Adding USSClass '{0}' to '{1}'", name, instance.GetType().Name);
                    element.AddToClassList(name);
                }
            },
            true);
        }
        
        public static void HandleUXMLReferenceAttributes<T>(T instance, VisualElement element)
        {
            instance.ForFieldWithAttr<T, UXMLReferenceAttribute>((info, attr) => {
                string uxmlName = attr.name;
                if (string.IsNullOrEmpty(uxmlName)) uxmlName = info.Name;
                //Debug.LogFormat("Populating 'UXMLReferenceAttribute' on '{0}.{1}' by looking for UXML name '{2}'", instance.GetType().Name, info.Name, uxmlName);
                info.SetValue(instance, element.Q(uxmlName));
            },
            true);
        }

        public static void HandleQueryAttributes<T>(T instance, VisualElement element)
        {
            instance.ForMethodWithAttr<T, QueryAttribute>((info, attr) => {
                //Debug.LogFormat("Registering 'QueryAttribute' on '{0}.{1}'", instance.GetType().Name, info.Name);
                element.Query(attr.name, attr.classes).ForEach((ve) => {info.Invoke(instance, new object[] { ve });});
            },
            false);
            instance.ForMethodWithAttr<T, QAttribute>((info, attr) => {
                //Debug.LogFormat("Registering 'QAttribute' on '{0}.{1}'", instance.GetType().Name, info.Name);
                info.Invoke(instance, new object[] { element.Query(attr.name, attr.classes).First() });
            },
            false);
        }
        
        public static void AddManipulators<T>(T instance, VisualElement element)
        {
            IOnMouse mouseHandler = instance as IOnMouse;
            if (mouseHandler != null)
            {
                //Debug.LogFormat("Adding 'MouseManipulator' to '{0}'", instance.GetType().Name);
                element.AddManipulator(new RedOwlMouseManipulator(mouseHandler.IsContentDragger, mouseHandler.MouseFilters.ToArray()));
            }
            
            IOnContextMenu contextMenuHandler = instance as IOnContextMenu;
            if (contextMenuHandler != null)
            {
                //Debug.LogFormat("Adding 'ContextualMenuManipulator' to '{0}'", instance.GetType().Name);
                element.AddManipulator(new ContextualMenuManipulator(contextMenuHandler.OnContextMenu));
            }
            
            IOnKeyboard keyboardHandler = instance as IOnKeyboard;
            if (keyboardHandler != null)
            {
                //Debug.LogFormat("Adding 'KeyboardManipulator' to '{0}'", instance.GetType().Name);
                element.AddManipulator(new RedOwlKeyboardManipulator(keyboardHandler.KeyboardFilters.ToArray()));
            }
            
            IOnWheel wheelHandler = instance as IOnWheel;
            IOnZoom zoomHandler = instance as IOnZoom;
            if (wheelHandler != null || zoomHandler != null)
            {
                //Debug.LogFormat("Adding 'WheelManipulator' to '{0}'", instance.GetType().Name);
                element.AddManipulator(new RedOwlWheelManipulator());
            }
        }
        
        public static void RegisterUICallbacks<T>(T instance, VisualElement element)
        {
            instance.ForMethodWithAttr<T, UICallbackAttribute>((info, attr) => {
                if (attr.once)
                {
                    Debug.LogFormat("Adding Callback '{0}' to '{1}' once after {2} milliseconds", info, instance.GetType().Name, attr.interval);
                    element.schedule.Execute(() => {info.Invoke(instance, null);}).StartingIn(attr.interval);
                } else {
                    Debug.LogFormat("Adding Callback '{0}' to '{1}' every {2} milliseconds", info, instance.GetType().Name, attr.interval);
                    element.schedule.Execute(() => {info.Invoke(instance, null);}).Every(attr.interval).Resume();
                }
            },
            false);
        }
    }
}
