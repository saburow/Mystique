﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interactivity;

namespace Mystique.Views.Behaviors
{
    public class FrameworkActualWidthAction : TriggerAction<FrameworkElement>
    {
        public double Source
        {
            get { return (double)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Property.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(double), typeof(FrameworkActualWidthAction), new FrameworkPropertyMetadata(0.0));

        protected override void Invoke(object parameter)
        {
            Source = AssociatedObject.ActualWidth;
        }
    }
}
