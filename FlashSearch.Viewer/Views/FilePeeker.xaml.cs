using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using FlashSearch.Viewer.Services;

namespace FlashSearch.Viewer.Views
{
    public partial class FilePeeker : UserControl
    {
        public FilePeeker()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
            _foregroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("ForegroundBrush");
            _wordMatchForegroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("WordMatchForegroundBrush");
            _currentMatchedLineBackgroundBrush = (SolidColorBrush) Application.Current.MainWindow.FindResource("CurrentMatchedLineBackgroundBrush");

        }

        private void UpdateFileDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.PageWidth = 100.0;

            List<int> LineNumbers = new List<int>();

            foreach (LineInfo line in Lines)
            {
                double lineWidth = line.Content.Length * 7.0;
                
                if (lineWidth > doc.PageWidth)
                    doc.PageWidth = lineWidth;

                Paragraph p = new Paragraph();
                p.LineHeight = 1;

                List<MatchPosition> positions = line.Matches.ToList();
                try
                {
                    if (positions.Count > 0)
                    {
                        for (int i = 0; i < positions.Count; i++)
                        {
                            int lastIndex = i == 0 ? 0 : (positions[i - 1].Begin + positions[i - 1].Length);
                            string before = line.Content.Substring(lastIndex, positions[i].Begin - lastIndex);
                            string match = line.Content.Substring(positions[i].Begin, positions[i].Length);

                            p.Inlines.Add(new Run(before) {Foreground = _foregroundBrush});
                            p.Inlines.Add(new Bold(new Run(match)) {Foreground = _wordMatchForegroundBrush});
                        }

                        string after = line.Content.Substring(positions.Last().Begin + positions.Last().Length);
                        p.Inlines.Add(new Run(after) {Foreground = _foregroundBrush});
                    }
                    else
                    {
                        p.Inlines.Add(new Run(line.Content) {Foreground = _foregroundBrush});
                    }
                }
                catch (Exception)
                {
                    p.Inlines.Clear();
                    p.Inlines.Add(new Run(line.Content) {Foreground = _foregroundBrush});
                }
                


                LineNumbers.Add(line.LineNumber);
                doc.Blocks.Add(p);
            }

            RTB.Document = doc;
            LineNumbersListBox.ItemsSource = LineNumbers;
            FileScrollViewer.UpdateLayout();
            double progression = LineNumber / (double) doc.Blocks.Count;
            FileScrollViewer.ScrollToVerticalOffset(
                (FileContentViewer.ActualHeight * progression) - (FileScrollViewer.ActualHeight / 2.0));
        }

        void SelectCurrentLine()
        {
            if (Lines == null || !Lines.Any())
                return;
            
            for (int i = 0; i < Lines.Count(); i++)
            {
                if (Lines.ElementAt(i).LineNumber == LineNumber)
                {
                    RTB.Document.Blocks.ElementAt(i).Background = _currentMatchedLineBackgroundBrush;
                    break;
                }
                else
                {
                    RTB.Document.Blocks.ElementAt(i).Background = null;
                }

            }
            
        }

        #region DependencyProperties
        
        private static void LineNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filePeeker = (FilePeeker) d;
            if (filePeeker.LineNumber == 0)
                return;
            filePeeker.SelectCurrentLine();
        }
        
        private static void LinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var filePeeker = (FilePeeker) d;
            if (filePeeker.Lines == null)
                return;
            filePeeker.UpdateFileDocument();
            filePeeker.SelectCurrentLine();

        }

        
        public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(
            "FileName", typeof(string), typeof(FilePeeker), new PropertyMetadata(default(string)));

        public string FileName
        {
            get { return (string) GetValue(FileNameProperty); }
            set { SetValue(FileNameProperty, value); }
        }

        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register(
            "LineNumber", typeof(int), typeof(FilePeeker), new PropertyMetadata(default(int), LineNumberChanged));

        public int LineNumber
        {
            get { return (int) GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }

        public static readonly DependencyProperty LinesProperty = DependencyProperty.Register(
            "Lines", typeof(IEnumerable<LineInfo>), typeof(FilePeeker), new PropertyMetadata(default(IEnumerable<LineInfo>), LinesChanged));

        public IEnumerable<LineInfo> Lines
        {
            get { return (IEnumerable<LineInfo>) GetValue(LinesProperty); }
            set { SetValue(LinesProperty, value); }
        }
        
        private SolidColorBrush _foregroundBrush;
        private SolidColorBrush _wordMatchForegroundBrush;
        private SolidColorBrush _currentMatchedLineBackgroundBrush;
        
        #endregion

        private void FilePeeker_OnLoaded(object sender, RoutedEventArgs e)
        {
//            LoadDocumentAsync(FileName);
        }
    }
}
