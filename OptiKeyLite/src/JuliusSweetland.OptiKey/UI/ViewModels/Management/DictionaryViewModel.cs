using System.Collections.ObjectModel;
using System.Linq;
using OptiKey.Models;
using OptiKey.Services;
using log4net;
using Prism.Commands;
using Prism.Mvvm;

namespace OptiKey.UI.ViewModels.Management
{
    public class DictionaryViewModel : BindableBase
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly IDictionaryService dictionaryService;
        
        #endregion
        
        #region Ctor

        public DictionaryViewModel(IDictionaryService dictionaryService)
        {
            this.dictionaryService = dictionaryService;
        
            AddCommand = new DelegateCommand(Add, () => !string.IsNullOrEmpty(NewEntry));
            ToggleDeleteCommand = new DelegateCommand<string>(ToggleDelete, e => !string.IsNullOrEmpty(e));
        
            Load();
        }
        
        #endregion
        
        #region Properties

        private ObservableCollection<DictionaryEntryAndState> entries;
        public ObservableCollection<DictionaryEntryAndState> Entries
        {
            get { return entries; }
            set { SetProperty(ref entries, value); }
        }
        
        private string newEntry;
        public string NewEntry
        {
            get { return newEntry; }
            set 
            { 
                SetProperty(ref newEntry, value);
                AddCommand.RaiseCanExecuteChanged();
            }
        }

        public bool ChangesRequireRestart
        {
            get { return false; }
        }
        
        public DelegateCommand AddCommand { get; private set; }
        public DelegateCommand<string> ToggleDeleteCommand { get; private set; }        
        
        #endregion
        
        #region Methods

        private void Load()
        {
            var allDictionaryEntries = dictionaryService.GetAllEntries();
            Entries = allDictionaryEntries != null
                ? new ObservableCollection<DictionaryEntryAndState>(
                    allDictionaryEntries
                        .Select(e => new DictionaryEntryAndState {Entry = e.Entry})
                        .OrderBy(e => e.Entry)
                        .ToList())
                : null;
        }
        
        private void Add()
        {
            if(Entries != null
               && !Entries.Any(e => e.Entry == NewEntry))
            {
                Entries.Add(new DictionaryEntryAndState {Entry = NewEntry, Added = true});
            }
            
            NewEntry = null;
        }
        
        private void ToggleDelete(string entry)
        {
            if(Entries != null)
            {
                var match = Entries.FirstOrDefault(e => e.Entry == entry);
                if(match != null)
                {
                    match.Deleted = !match.Deleted;
                }
            }
        }

        public void ApplyChanges()
        {
            if(Entries != null)
            {
                //Add new entries
                foreach(var addedEntry in Entries.Where(e => e.Added).Select(e => e.Entry))
                {
                    dictionaryService.AddNewEntryToDictionary(addedEntry);
                }
                
                //Remove deleted entries
                foreach(var deletedEntry in Entries.Where(e => e.Deleted).Select(e => e.Entry))
                {
                    dictionaryService.RemoveEntryFromDictionary(deletedEntry);
                }
            }
        }

        #endregion
    }
}
