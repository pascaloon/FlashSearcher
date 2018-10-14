using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Autofac;
using FlashSearch.Viewer.Services;

namespace FlashSearch.Viewer.Views
{
    public partial class FilePeeker : UserControl
    {
        private readonly FileService _fileService;
        public ObservableCollection<int> LineNumbers { get; set; }

        
        public FilePeeker()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
            _fileService = GlobalFactory.Container.Resolve<FileService>();
            LineNumbers = new ObservableCollection<int>();

        }

        
        private void LoadDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.PageWidth = 1000;
            LineNumbers.Clear();   
            foreach (LineInfo line in _fileService.GetContextLines(FileName, LineNumber, ContextAmount, Regex))
            {
                Paragraph p = new Paragraph();
                p.LineHeight = 1;

                if (line.LineNumber == LineNumber)
                {
                    p.Background = Brushes.LightBlue;
                }
                
                List<MatchPosition> positions = line.Matches.ToList();
                if (positions.Count > 0)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        int lastIndex = i == 0 ? 0 : (positions[i - 1].Begin + positions[i - 1].Length);
                        string before = line.Line.Substring(lastIndex, positions[i].Begin - lastIndex);
                        string match = line.Line.Substring(positions[i].Begin, positions[i].Length);
                    
                        p.Inlines.Add(new Run(before));
                        p.Inlines.Add(new Bold(new Run(match)) {Foreground = Brushes.Green});
                    }
                    string after = line.Line.Substring(positions.Last().Begin + positions.Last().Length);
                    p.Inlines.Add(after);
                }
                else
                {
                    p.Inlines.Add(line.Line);
                }
                
                
                LineNumbers.Add(line.LineNumber);
                doc.Blocks.Add(p);

            }

            RTB.Document = doc;
        }

        #region DependencyProperties

        private static void CustomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filePeeker = (FilePeeker) d;
            filePeeker.OnFileCanged();
        }
        
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
            "FileName", typeof(string), typeof(FilePeeker), new PropertyMetadata(default(string), CustomPropertyChanged));

        public string FileName
        {
            get { return (string) GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
            "LineNumber", typeof(int), typeof(FilePeeker), new PropertyMetadata(default(int), CustomPropertyChanged));

        public int LineNumber
        {
            get { return (int) GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }

        public static readonly DependencyProperty ContextAmountProperty = DependencyProperty.Register(
            "ContextAmount", typeof(int), typeof(FilePeeker), new PropertyMetadata(default(int), CustomPropertyChanged));

        public int ContextAmount
        {
            get { return (int) GetValue(ContextAmountProperty); }
            set { SetValue(ContextAmountProperty, value); }
        }

        public static readonly DependencyProperty RegexProperty = DependencyProperty.Register(
            "Regex", typeof(Regex), typeof(FilePeeker), new PropertyMetadata(default(Regex)));

        public Regex Regex
        {
            get { return (Regex) GetValue(RegexProperty); }
            set { SetValue(RegexProperty, value); }
        }
        
        #endregion

        private void FilePeeker_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadDocument();
        }

        public void OnFileCanged()
        {
            if (!this.IsLoaded)
                return;
            LoadDocument();
        }
    }
}
