﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AP = Avalonia.AvaloniaProperty;

// ReSharper disable UnusedParameter.Local
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Editor.UI.Inspector;

public partial class InspectorTag : UserControl
{
    public static readonly StyledProperty<string>   TagNameProperty  = AP.Register<InspectorTag, string>(nameof(TagName), "Tag");
    
    public string   TagName  { get => GetValue(TagNameProperty);  set => SetValue(TagNameProperty, value); }
    
    public InspectorTag()
    {
        InitializeComponent();
    }

    private void MenuItem_RemoveTag(object sender, RoutedEventArgs e) {
        Console.WriteLine("MenuItem_RemoveTag");
    }
}