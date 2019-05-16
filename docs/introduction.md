---
layout: default
nav_order: 2
---

## Introduction
---

First lets start out with an example VisualElement that you'd write if you didn't use this library and then we'll show that same example VisualElement if you did write it with this library so you can see how much boilerplate code goes way.

Given these UXML and USS files below the code turns them into an element that can pan around its child element with right mouse click.

#### Source UXML and USS for examples

```xml
<?xml version="1.0" encoding="utf-8"?>
<UXML xmlns="UnityEngine.UIElements">
    <VisualElement name="content" class="fill">
        <VisualElement name="frame">
            <VisualElement name="texture" class="logo" />
        </VisualElement>
    </VisualElement>
</UXML>
```

```css
.fill {
    position: absolute;
    top: 0px;
    bottom: 0px;
    right: 0px;
    left: 0px;
}
.logo {
    background-image: Resource("RedOwl/Demo/Logo")
}
```

#### The Unity Only Way

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace RedOwl.Demo {
    public class PanManipulator : MouseManipulator {
        private Action<Vector2> callback;
		private Vector2 _mouseStart;
		private bool _active;

		public PanManipulator(Action<Vector2> callback, params ManipulatorActivationFilter[] filters)
		{
            base()
            foreach (var filter in filters)
            {
                activators.Add(filter)
            }
			this.callback = callback;
			_active = false;
		}
		
		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseDownEvent>(OnMouseDown);
			target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
			target.RegisterCallback<MouseUpEvent>(OnMouseUp);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
			target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
			target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
		}
		
		protected void OnMouseDown(MouseDownEvent evt)
		{
			if (_active)
			{
				evt.StopImmediatePropagation();
				return;
			}

			if (CanStartManipulation(evt))
			{
				_mouseStart = evt.localMousePosition;
				_active = true;
				target.CaptureMouse();
				evt.StopPropagation();
			}
		}

		protected void OnMouseMove(MouseMoveEvent evt)
		{
			if (!_active || !target.HasMouseCapture()) return;
			callback(evt.localMousePosition - _mouseStart);
			_mouseStart = evt.localMousePosition;
			evt.StopPropagation();
		}

		protected void OnMouseUp(MouseUpEvent evt)
		{
			if (!_active || !target.HasMouseCapture() || !CanStopManipulation(evt)) return;
			_active = false;
			target.ReleaseMouse();
			evt.StopPropagation();
		}
    }

    public class Demo : EditorWindow
    {
        const string uxmlPath = "RedOwl/Demo/DemoLayout";
        const string ussPath = "RedOwl/Demo/DemoStyle";

        VisualElement frame;

        [MenuItem("Tools/Unity")]
        public static void Open()
        {
            var wnd = GetWindow<Demo>();
        }

        public void OnEnable()
        {
            var root = this.GetRootVisualContainer();
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPAth);
            visualTree.CloneTree(root, null);
            root.AddStyleSheetPath(ussPath);

            frame = root.Q("frame");

            root.AddManipulator(OnPath, new ManipulatorActivationFilter { button = MouseButton.RightMouse})
        }
        
        public void OnPan(Vector2 delta)
        {
            Vector3 current = frame.transform.position;
            frame.transform.position = new Vector3(current.x + delta.x, current.y + delta.y, -100f);
        }
    }
}
```

#### The Red Owl Way

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RedOwl.Editor;

namespace RedOwl.Demo
{
    [UXML, USS]
    public class Demo : RedOwlEditorWindow<Demo>, IOnMouse
    {
        [UXMLReference]
        VisualElement frame;

        [MenuItem("Tools/RedOwl")]
        public static void Open()
        {
            EnsureWindow();
        }
        
        public IEnumerable<MouseFilter> MouseFilters {
            get {
                yield return new MouseFilter { 
                    button = MouseButton.RightMouse,
                    OnMove = OnPan
                };
            }
        }
        
        public void OnPan(MouseMoveEvent evt, Vector2 delta)
        {
            Vector3 current = frame.transform.position;
            frame.transform.position = new Vector3(current.x + delta.x, current.y + delta.y, -100f);
        }
    }
}
```

There are 104 lines of code in the Unity way and there are only 34 in the RedOwl way but both achive the same end result.

On top of reducing the redundent code it also help you keep a very clear seperation of concerns, your C# is truely closer to being just business logic instead of being both business logic and ui hookup / input handling hookup code.

<blockquote class="label bg-grey-dk-100">But I don't wanna use your base classes!!!</blockquote>

Well then you are in luck, you don't have to - if you looked at the `RedOwlVisualElement` base class you'd notice that its very sparse.  Thats because all that sweet sweet functionality is offloaded into a utility function so you can do this if you need to.

#### Unity + RedOwl

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RedOwl.Editor;

public class DemoElement : VisualElement, IOnMouse
{
    VisualElement frame;
 
    public DemoElement()
    {
        RedOwlUtils.Setup(this, this);  //Here is where the magic happens
        frame = new VisualElement();
    }
 
    public IEnumerable<MouseFilter> MouseFilters {
        get {
            yield return new MouseFilter {
                button = MouseButton.RightMouse,
                OnMove = OnPan
            };
        }
    }
 
    private void OnPan(MouseMoveEvent evt, Vector2 delta)
    {
        Vector3 current = frame.transform.position;
        frame.transform.position = new Vector3(current.x + delta.x, current.y + delta.y, -100f);
    }
}
```

So from here on out when we refer to `RedOwlClasses` we mean any class that has been run through the `RedOwlUtils.Setup` function.  The RedOwl variants of Unity's classes all have this called on them at the appropreiate time and place so you don't have to worry about it.
