using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashSearch.Viewer.ViewModels
{
    class FileNotFoundViewModel : ViewModelBase
    {
        private readonly SearchResult _searchResult;

        private String _file;
        public String File
        {
            get { return _file; }
            set { Set(ref _file, value); }
        }

        public FileNotFoundViewModel(SearchResult searchResult)
        {
            _searchResult = searchResult;
            _file = _searchResult.FileInfo.FullName;
        }
    }
}
