using System;
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
            doc.PageWidth = 100.0;
            LineNumbers.Clear();
            
            SolidColorBrush foregroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("ForegroundBrush");
            SolidColorBrush wordMatchForegroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("WordMatchForegroundBrush");
            SolidColorBrush currentMatchedLineBackgroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("CurrentMatchedLineBackgroundBrush");
            
            foreach (LineInfo line in _fileService.GetContextLines(FileName, LineNumber, ContextAmount, ContentSelector))
            {
                double lineWidth = line.Content.Length * 7.0;
                if (lineWidth > doc.PageWidth)
                    doc.PageWidth = lineWidth;

                Paragraph p = new Paragraph();
                p.LineHeight = 1;

                if (line.LineNumber == LineNumber)
                {
                    p.Background = currentMatchedLineBackgroundBrush;
                }
                
                List<MatchPosition> positions = line.Matches.ToList();
                if (positions.Count > 0)
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        int lastIndex = i == 0 ? 0 : (positions[i - 1].Begin + positions[i - 1].Length);
                        string before = line.Content.Substring(lastIndex, positions[i].Begin - lastIndex);
                        string match = line.Content.Substring(positions[i].Begin, positions[i].Length);
                    
                        p.Inlines.Add(new Run(before) {Foreground = foregroundBrush});
                        p.Inlines.Add(new Bold(new Run(match)) {Foreground = wordMatchForegroundBrush});
                    }
                    string after = line.Content.Substring(positions.Last().Begin + positions.Last().Length);
                    p.Inlines.Add(new Run(after){Foreground = foregroundBrush});
                }
                else
                {
                    p.Inlines.Add(new Run(line.Content){Foreground = foregroundBrush});
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

        public static readonly DependencyProperty ContentSelectorProperty = DependencyProperty.Register(
            "ContentSelector", typeof(IContentSelector), typeof(FilePeeker), new PropertyMetadata(default(IContentSelector)));

        public IContentSelector ContentSelector
        {
            get { return (IContentSelector) GetValue(ContentSelectorProperty); }
            set { SetValue(ContentSelectorProperty, value); }
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
