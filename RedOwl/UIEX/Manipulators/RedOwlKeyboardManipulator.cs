﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RedOwl.Editor
{
	public interface IOnKeyboard {
		IEnumerable<KeyboardFilter> KeyboardFilters { get; }
	}
	
	public struct KeyboardFilter
	{
		public KeyCode key;
		public EventModifiers modifiers;
		
		public Action<KeyDownEvent> OnDown;
		public Action<KeyUpEvent> OnUp;
		
		public KeyboardFilter(KeyCode key, EventModifiers modifiers, Action<KeyDownEvent> OnDown, Action<KeyUpEvent> OnUp)
		{
			this.key = key;
			this.modifiers = modifiers;
			
			this.OnDown = OnDown;
			this.OnUp = OnUp;
		}

		public bool Matches(IKeyboardEvent e)
		{
			return key == e.keyCode && HasModifiers(e);
		}

		private bool HasModifiers(IKeyboardEvent e)
		{
			if (((modifiers & EventModifiers.Alt) != 0 && !e.altKey) ||
			((modifiers & EventModifiers.Alt) == 0 && e.altKey))
			{
				return false;
			}

			if (((modifiers & EventModifiers.Control) != 0 && !e.ctrlKey) ||
			((modifiers & EventModifiers.Control) == 0 && e.ctrlKey))
			{
				return false;
			}

			if (((modifiers & EventModifiers.Shift) != 0 && !e.shiftKey) ||
			((modifiers & EventModifiers.Shift) == 0 && e.shiftKey))
			{
				return false;
			}

			return ((modifiers & EventModifiers.Command) == 0 || e.commandKey) &&
			((modifiers & EventModifiers.Command) != 0 || !e.commandKey);
		}
	}
	
	public class RedOwlKeyboardManipulator : Manipulator
	{
		private KeyboardFilter[] filters;
		public KeyboardFilter currentFilter { get; private set; }

		public RedOwlKeyboardManipulator(params KeyboardFilter[] filters)
		{
			this.filters = filters;
		}
		
		protected override void RegisterCallbacksOnTarget()
		{
			target.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
			target.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
			target.RegisterCallback<KeyDownEvent>(OnKeyDown);
			target.RegisterCallback<KeyUpEvent>(OnKeyUp);
		}

		protected override void UnregisterCallbacksFromTarget()
		{
			target.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
			target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
			target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
			target.UnregisterCallback<KeyUpEvent>(OnKeyUp);
		}
		
		void OnMouseEnter(MouseEnterEvent evt)
		{
			target.Focus();
			evt.StopPropagation();
		}
		
		void OnMouseLeave(MouseLeaveEvent evt)
		{
			target.Blur();
			evt.StopPropagation();
		}
		
		protected void OnKeyDown(KeyDownEvent evt)
		{
			if (CanStartManipulation(evt) && currentFilter.OnDown != null) currentFilter.OnDown(evt);
		}

		protected void OnKeyUp(KeyUpEvent evt)
		{
			if (!CanStopManipulation(evt) && currentFilter.OnUp != null) currentFilter.OnUp(evt);
		}

		private bool CanStartManipulation(IKeyboardEvent evt)
		{
			foreach (var filter in filters)
			{
				if (filter.Matches(evt))
				{
					currentFilter = filter;
					return true;
				}
			}
			return false;
		}

		private bool CanStopManipulation(IKeyboardEvent e)
		{
			return (e.keyCode == currentFilter.key);
		}
	}
}