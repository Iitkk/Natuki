﻿namespace Natuki
{
    using NatukiLib.Utils;
    using NatukiLib.ViewModels;
    using ScottPlot;
    using System;
    using System.Linq;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            System.Drawing.Color? defaultColor = null;
            System.Drawing.Color? defaultNegativeColor = null;

            DataContext = new MainViewModel(vm =>
                {
                    var xValues = vm.ViewDataXValues;
                    var plt = new Plot();
                    if (xValues.Length > 0)
                    {
                        var yValues = vm.ViewDataYValues;
                        var viewDataType = vm.ViewDataType;
                        var bar = plt.AddBar(yValues, xValues);
                        plt.YLabel(vm.ViewDataTypeText);
                        plt.XLabel(vm.ViewDataStoryText);
                        if (!defaultColor.HasValue) defaultColor = bar.FillColor;
                        if (!defaultNegativeColor.HasValue) defaultNegativeColor = bar.FillColorNegative;
                        double yMinBound;
                        if (viewDataType == ViewDataType.AbandonmentRate || viewDataType == ViewDataType.AbandonmentRateToNextStory)
                        {
                            bar.FillColor = defaultNegativeColor.Value;
                            bar.FillColorNegative = defaultColor.Value;
                            var minValue = yValues.Min() * 3;
                            yMinBound = Math.Min(0, minValue);
                        }
                        else
                            yMinBound = 0;

                        #region X軸設定

                        plt.XAxis.Ticks(true, false);
                        // Format https://tinyurl.com/y86clj9k
                        if (viewDataType == ViewDataType.UniqueAccessByDate)
                        {
                            var xMin = xValues.Min();
                            var xMax = xValues.Max();
                            plt.XAxis.SetBoundary(xMin, xMax + (xMax - xMin) * 1.25);
                            plt.XAxis.TickLabelFormat("yyyy/M/d", dateTimeFormat: true);
                        }
                        else
                        {
                            plt.XAxis.SetBoundary(0, xValues.Max() * 2.25);
                            plt.XAxis.MinimumTickSpacing(1);
                            plt.XAxis.TickLabelFormat("F0", dateTimeFormat: false);
                        }

                        #endregion

                        #region Y軸設定

                        var yValueMax = yValues.Max();
                        plt.YAxis.SetBoundary(yMinBound, yValueMax * 2.25);
                        plt.YAxis.Ticks(true, false);
                        if (viewDataType == ViewDataType.SubtotalUniqueAccess || viewDataType == ViewDataType.UniqueAccessByDate)
                        {
                            plt.YAxis.TickLabelFormat("N0", dateTimeFormat: false);
                            plt.YAxis.MinimumTickSpacing(1);
                        }
                        else
                        {
                            plt.YAxis.TickLabelFormat(yValueMax > 0.05 ? "P0" : "P1", dateTimeFormat: false);
                        }

                        #endregion
                    }
                    MainWpfPlot.Reset(plt);
                    MainWpfPlot.Refresh();
                }, x =>
                {
                    if (x is object[] values)
                    {
                        var message = (string)values[0];
                        var title = (string)values[1];
                        if ((bool)values[2])
                            return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                        else
                        {
                            MessageBox.Show(message, title, MessageBoxButton.OK);
                            return null;
                        }
                    }
                    else
                        return null;
                }
                );
        }
    }
}
