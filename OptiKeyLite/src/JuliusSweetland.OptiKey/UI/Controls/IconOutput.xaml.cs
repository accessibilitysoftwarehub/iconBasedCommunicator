using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OptiKey.UI.Controls
{
    /// <summary>
    /// Interaction logic for IconOutput.xaml
    /// </summary>
    public partial class IconOutput : UserControl
    {
        public IconOutput()
        {
            InitializeComponent();
            Loaded += (sender, args) => NumberOfSuggestionsDisplayed = 4;
        }

        public static readonly DependencyProperty NumberOfSuggestionsDisplayedProperty =
            DependencyProperty.Register("NumberOfSuggestionsDisplayed", typeof(int), typeof(IconOutput), new PropertyMetadata(default(int)));

        public int NumberOfSuggestionsDisplayed
        {
            get { return (int)GetValue(NumberOfSuggestionsDisplayedProperty); }
            set { SetValue(NumberOfSuggestionsDisplayedProperty, value); }
        }

        public static readonly DependencyProperty ScratchpadWidthInKeysProperty = DependencyProperty.Register(
            "ScratchpadWidthInKeys", typeof(int), typeof(IconOutput), new PropertyMetadata(default(int)));

        public int ScratchpadWidthInKeys
        {
            get { return (int)GetValue(ScratchpadWidthInKeysProperty); }
            set { SetValue(ScratchpadWidthInKeysProperty, value); }
        }
    }
}
