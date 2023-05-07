namespace Natuki
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
                        plt.XLabel("部分");
                        if (!defaultColor.HasValue) defaultColor = bar.FillColor;
                        if (!defaultNegativeColor.HasValue) defaultNegativeColor = bar.FillColorNegative;
                        double yMinBound;
                        if (viewDataType == ViewDataType.AbandonmentRate || viewDataType == ViewDataType.AbandonmentRateOnPreviousStory)
                        {
                            bar.FillColor = defaultNegativeColor.Value;
                            bar.FillColorNegative = defaultColor.Value;
                            var minValue = yValues.Min() * 3;
                            yMinBound = Math.Min(0, minValue);
                        }
                        else
                            yMinBound = 0;

                        #region 軸設定

                        plt.XAxis.SetBoundary(0, xValues.Max() * 2.25);
                        plt.XAxis.MinimumTickSpacing(1);
                        plt.XAxis.Ticks(true, false);
                        // Format https://tinyurl.com/y86clj9k
                        plt.XAxis.TickLabelFormat("F0", dateTimeFormat: false);
                        plt.YAxis.SetBoundary(yMinBound, yValues.Max() * 2.25);
                        plt.YAxis.Ticks(true, false);
                        if (viewDataType != ViewDataType.UniqueAccess)
                            plt.YAxis.TickLabelFormat("P0", dateTimeFormat: false);
                        else
                        {
                            plt.YAxis.TickLabelFormat("N0", dateTimeFormat: false);
                            plt.YAxis.MinimumTickSpacing(1);
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
