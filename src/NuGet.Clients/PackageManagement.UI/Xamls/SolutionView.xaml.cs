// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;

namespace NuGet.PackageManagement.UI
{
    /// <summary>
    /// Interaction logic for SolutionView.xaml
    /// </summary>
    public partial class SolutionView : UserControl
    {
        public event EventHandler<EventArgs> InstallButtonClicked;

        public event EventHandler<EventArgs> UninstallButtonClicked;

        public SolutionView()
        {
            InitializeComponent();
        }

        private void UninstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (UninstallButtonClicked != null)
            {
                UninstallButtonClicked(this, EventArgs.Empty);
            }
        }

        private void InstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (InstallButtonClicked != null)
            {
                InstallButtonClicked(this, EventArgs.Empty);
            }
        }
    }
}