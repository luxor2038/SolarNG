using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SolarNG.Configs;
using SolarNG.Sessions;

namespace SolarNG.ViewModel.Settings;

public class EditTagViewModel : ViewModelBase, INotifyPropertyChanged, INotifyDataErrorInfo
{
    public TagsListViewModel TagsListVM;

    public Brush TitleBackground { get; set; }

    public bool BatchMode { get; set; }

    public bool NewMode { get; set; }

    public bool EditMode => !BatchMode && !NewMode;

    public bool ControlVisible { get; set; }

    public Session EditedTag { get; set; } = new Session("tag");

    public string Name
    {
        get
        {
            return EditedTag.Name;
        }
        set
        {
            EditedTag.Name = value;
            NotifyPropertyChanged("Name");
        }
    }

    private bool _NotInOverviewCheckThree = false;
    public bool NotInOverviewCheckThree => BatchMode && _NotInOverviewCheckThree;

    private Nullable<bool> _NotInOverviewCheck;
    public Nullable<bool> NotInOverviewCheck
    {
        get
        {
            if (BatchMode)
            {
                return _NotInOverviewCheck;
            }

            return (EditedTag.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
        }
        set
        {
            _NotInOverviewCheck = value;

            if (value != null && value.Value)
            {
                EditedTag.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            else
            {
                EditedTag.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
            }
            NotifyPropertyChanged("NotInOverviewCheck");
        }
    }

    private Dictionary<Guid, string> InitialTags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> Tags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> AddedTags = new Dictionary<Guid, string>();
    private Dictionary<Guid, string> RemovedTags = new Dictionary<Guid, string>();

    public ObservableCollection<ComboBoxOne> AssignedTags { get; set; } = new ObservableCollection<ComboBoxOne>();
    private void CreateTags()
    {
        InitialTags.Clear();
        Tags.Clear();
        AddedTags.Clear();
        RemovedTags.Clear();
        AssignedTags.Clear();
        UnassignedTags.Clear();

        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            if(SelectedTag != null && SelectedTag.Tags != null && SelectedTag.Tags.Contains(tag.Name))
            {
                AssignedTags.Add(new ComboBoxOne(tag.Name));
                InitialTags[tag.RuntimeId] = tag.Name;
                Tags[tag.RuntimeId] = tag.Name;
            }
            else if(SelectedTag == null || !SelectedTags.Contains(tag))
            {
                if(SelectedTags != null)
                {
                    bool found = false;
                    foreach(Session tagSession in SelectedTags)
                    {
                        if(tagSession.ChildSessions.Contains(tag))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        continue;
                    }
                }

                UnassignedTags.Add(new ComboBoxOne(tag.Name));
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag2 = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag2");
    }

    private ObservableCollection<ComboBoxOne> OrderList(ObservableCollection<ComboBoxOne> list)
    {
        if(list.Count == 0)
        {
            return list;
        }

        return new ObservableCollection<ComboBoxOne>(list.OrderBy((ComboBoxOne s) => s.Key));
    }

    public RelayCommand<string> DeleteAssignedTagCommand { get; set; }
    private void OnDeleteAssignedTag(string tagName)
    {
        Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.Type == "tag" && s.Name == tagName);

        foreach(Session childSession in tagSession.ChildSessions.Where(s => s.Type == "tag"))
        {
            if(Tags.ContainsKey(childSession.RuntimeId))
            {
                return;
            }
        }
        
        if(InitialTags.ContainsKey(tagSession.RuntimeId))
        {
            RemovedTags[tagSession.RuntimeId] = tagName;
        }
        AddedTags.Remove(tagSession.RuntimeId);
        Tags.Remove(tagSession.RuntimeId);

        ComboBoxOne item = AssignedTags.FirstOrDefault(s => s.Key == tagName);
        AssignedTags.Remove(item);
        UnassignedTags.Add(item);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag2 = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag2");
    }

    public ComboBoxOne SelectedTag2 { get; set; }

    public ObservableCollection<ComboBoxOne> UnassignedTags { get; set; } = new ObservableCollection<ComboBoxOne>();

    public RelayCommand AssignCommand { get; set; }
    private void OnAssignTag()
    {
        if (SelectedTag2 == null)
        {
            return;
        }

        Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.Type == "tag" && s.Name == SelectedTag2.Key);
        Dictionary<Guid, string> tags = new Dictionary<Guid, string>();

        tags = GetParentTags(tagSession, tags);

        foreach(Guid tagId in tags.Keys)
        {
            if(!InitialTags.ContainsKey(tagId))
            {
                AddedTags[tagId] = tags[tagId];
            }
            RemovedTags.Remove(tagId);

            if(!Tags.ContainsKey(tagId))
            {
                Tags[tagId] = tags[tagId];
            }

            ComboBoxOne item = UnassignedTags.FirstOrDefault(t => t.Key == tags[tagId]);
            if(item != null)
            {
                UnassignedTags.Remove(item);
                AssignedTags.Add(item);
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag2 = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag2");
    }

    private Dictionary<Guid, string> GetParentTags(Session tag, Dictionary<Guid, string> tags)
    {
        tags[tag.RuntimeId] = tag.Name;
        
        foreach(Session parentTag in App.Sessions.Sessions.Where(s => s.ChildSessions.Contains(tag)))
        {
            tags = GetParentTags(parentTag, tags);
        }

        return tags;
    }

    public string Comment
    {
        get
        {
            return EditedTag.Comment;
        }
        set
        {
            EditedTag.Comment = value;
            NotifyPropertyChanged("Comment");
        }
    }

    private Visibility _OkValidationVisibility;
    public Visibility OkValidationVisibility
    {
        get
        {
            return _OkValidationVisibility;
        }
        set
        {
            _OkValidationVisibility = value;
            NotifyPropertyChanged("OkValidationVisibility");
        }
    }

    public RelayCommand SaveCommand { get; set; }
    private void OnSaveTag()
    {
        if (!InputIsValid())
        {
            return;
        }

        if(NewMode)
        {
            Session tag = SaveTag();

            TagsListVM.SelectItem(tag);
            TagsListVM.ListUpdate();
        }

        if(BatchMode)
        {
            SaveTags();
        }
        else
        {
            SaveTag();
        }

        CreateTags();
    }

    private Session SaveTag()
    {
        Session tag = SelectedTag??new Session("tag");

        if(SelectedTag != null && tag.Name != EditedTag.Name)
        {
            foreach (Session session in SelectedTag.ChildSessions)
            {
                session.Tags.Remove(tag.Name);
                session.Tags.Add(EditedTag.Name);
            }
        }

        tag.Name = Name;
        tag.Comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

        if(NotInOverviewCheck != null && NotInOverviewCheck.Value)
        {
            tag.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
        }
        else
        {
            tag.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
        }

        tag.Tags ??= new ObservableCollection<string>();
        foreach(string tagName in AddedTags.Values)
        {
            tag.Tags.Add(tagName);
        }
        foreach(string tagName in RemovedTags.Values)
        {
            tag.Tags.Remove(tagName);
        }
        if(tag.Tags.Count == 0)
        {
            tag.Tags = null;
        }

        foreach(Guid tagId in AddedTags.Keys)
        {
            Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.RuntimeId == tagId);

            List<Session> tags = new List<Session>();

            tags = GetParentTags(tagSession, tags);

            foreach(Session parentTag in tags)
            {
                parentTag.ChildSessions.CollectionChanged -= UpdateSessions2;

                if(!parentTag.ChildSessions.Contains(tag))
                {
                    parentTag.ChildSessions.Add(tag);
                }

                foreach(Session session in tag.ChildSessions)
                {
                    if(!parentTag.ChildSessions.Contains(session))
                    {
                        parentTag.ChildSessions.Add(session);
                    }
                }

                parentTag.OnPropertyChanged("CredentialName");

                parentTag.ChildSessions.CollectionChanged += UpdateSessions2;
            }
        }

        foreach(Session tagSession in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tagSession.ChildSessions.CollectionChanged -= UpdateSessions2;

            if(RemovedTags.ContainsKey(tagSession.RuntimeId))
            {
                tagSession.ChildSessions.Remove(tag);
                tagSession.OnPropertyChanged("CredentialName");
            }

            tagSession.ChildSessions.CollectionChanged += UpdateSessions2;
        }

        if(SelectedTag == null)
        {
            App.Sessions.Sessions.Add(tag);
        }
        else
        {
            App.RefreshOverview();
        }

        return tag;
    }

    private List<Session> GetParentTags(Session tag, List<Session> tags)
    {
        tags.Add(tag);
        
        foreach(Session parentTag in App.Sessions.Sessions.Where(s => s.ChildSessions.Contains(tag)))
        {
            tags = GetParentTags(parentTag, tags);
        }

        return tags;
    }

    private void SaveTags()
    {
        foreach(Session tag in SelectedTags)
        {
            if(NotInOverviewCheck != null)
            {
                if(NotInOverviewCheck.Value)
                {
                    tag.iFlags |= ProgramConfig.FLAG_NOTINOVERVIEW;
                }
                else
                {
                    tag.iFlags &= ~ProgramConfig.FLAG_NOTINOVERVIEW;
                }
            }

            tag.Tags ??= new ObservableCollection<string>();
            foreach(string tagName in AddedTags.Values)
            {
                if(!tag.Tags.Contains(tagName))
                {
                    tag.Tags.Add(tagName);
                }
            }
            foreach(string tagName in RemovedTags.Values)
            {
                tag.Tags.Remove(tagName);
            }
            if(tag.Tags.Count == 0)
            {
                tag.Tags = null;
            }

            foreach(Guid tagId in AddedTags.Keys)
            {
                Session tagSession = App.Sessions.Sessions.FirstOrDefault(s => s.RuntimeId == tagId);

                List<Session> tags = new List<Session>();

                tags = GetParentTags(tagSession, tags);

                foreach(Session parentTag in tags)
                {
                    parentTag.ChildSessions.CollectionChanged -= UpdateSessions2;

                    if(!parentTag.ChildSessions.Contains(tag))
                    {
                        parentTag.ChildSessions.Add(tag);
                    }

                    foreach(Session session in tag.ChildSessions)
                    {
                        if(!parentTag.ChildSessions.Contains(session))
                        {
                            parentTag.ChildSessions.Add(session);
                        }
                    }

                    parentTag.OnPropertyChanged("CredentialName");

                    parentTag.ChildSessions.CollectionChanged += UpdateSessions2;
                }
            }

            foreach(Session tagSession in App.Sessions.Sessions.Where(s => s.Type == "tag"))
            {
                tagSession.ChildSessions.CollectionChanged -= UpdateSessions2;

                if(RemovedTags.ContainsKey(tagSession.RuntimeId))
                {
                    tagSession.ChildSessions.Remove(tag);
                    tagSession.OnPropertyChanged("CredentialName");
                }

                tagSession.ChildSessions.CollectionChanged += UpdateSessions2;
            }

            if(EditedTag.Comment != "!NoChange!")
            {
                tag.Comment = string.IsNullOrWhiteSpace(EditedTag.Comment) ? null : EditedTag.Comment.Trim();
            }
        }

        SelectedTag.Tags.Clear();
        foreach(string tagName in Tags.Values)
        {
            SelectedTag.Tags.Add(tagName);
        }

        App.RefreshOverview();
    }

    public EditTagViewModel()
    {
        DeleteAssignedTagCommand = new RelayCommand<string>(OnDeleteAssignedTag);
        AssignCommand = new RelayCommand(OnAssignTag);
        SaveCommand = new RelayCommand(OnSaveTag);

        App.Sessions.Sessions.CollectionChanged += UpdateSessions;
        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged += UpdateSessions2;
            tag.NameChange += UpdateTag;
        }

        UpdateGUI(Visibility.Hidden);
    }

    public override void Cleanup()
    {
        App.Sessions.Sessions.CollectionChanged -= UpdateSessions;

        foreach (Session tag in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            if(tag.GetNameChange() != null)
            {
                tag.NameChange -= UpdateTag;
            }
        }

        base.Cleanup();
    }

    private void UpdateSessions(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        foreach (Session tag in App.Sessions.Sessions.Where(s => s.Type == "tag"))
        {
            tag.ChildSessions.CollectionChanged -= UpdateSessions2;
            tag.ChildSessions.CollectionChanged += UpdateSessions2;

            if(tag.GetNameChange() != null)
            {
                tag.NameChange -= UpdateTag;
            }
            tag.NameChange += UpdateTag;
        }
        UpdateSessions2(sender, notifyCollectionChangedEventArgs);
    }

    private void UpdateTag(object sender, EventArgs e)
    {
        UpdateSessions2(sender, null);
    }

    private void UpdateSessions2(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        Dictionary<Guid, string> tags = new Dictionary<Guid, string>(Tags);
        Dictionary<Guid, string> addedTags = new Dictionary<Guid, string>(AddedTags);
        Dictionary<Guid, string> removedTags = new Dictionary<Guid, string>(RemovedTags);

        InitialTags.Clear();
        Tags.Clear();
        AddedTags.Clear();
        RemovedTags.Clear();
        AssignedTags.Clear();
        UnassignedTags.Clear();

        foreach (Session tag in App.Sessions.Sessions.Where((Session s) => s.Type == "tag"))
        {
            if(tags.ContainsKey(tag.RuntimeId))
            {
                Tags[tag.RuntimeId] = tag.Name;
            }

            if(addedTags.ContainsKey(tag.RuntimeId))
            {
                AddedTags[tag.RuntimeId] = tag.Name;
            }

            if(removedTags.ContainsKey(tag.RuntimeId))
            {
                RemovedTags[tag.RuntimeId] = tag.Name;
            }

            if(SelectedTag != null && SelectedTag.Tags != null && SelectedTag.Tags.Contains(tag.Name))
            {
                AssignedTags.Add(new ComboBoxOne(tag.Name));
                InitialTags[tag.RuntimeId] = tag.Name;
            }
            else
            {
                if(SelectedTag == null || !SelectedTags.Contains(tag))
                {
                    if(SelectedTags != null)
                    {
                        bool found = false;
                        foreach(Session tagSession in SelectedTags)
                        {
                            if(tagSession.ChildSessions.Contains(tag))
                            {
                                found = true;
                                break;
                            }
                        }
                        if(found)
                        {
                            continue;
                        }
                    }

                    UnassignedTags.Add(new ComboBoxOne(tag.Name));
                }
            }
        }

        AssignedTags = OrderList(AssignedTags);
        UnassignedTags = OrderList(UnassignedTags);
        SelectedTag2 = UnassignedTags.FirstOrDefault();

        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag2");
    }

    private List<Session> SelectedTags;
    public void ShowSelectedTags(List<Session> tags)
    {
        SelectedTags = tags;

        if(tags.Count == 1)
        {
            ShowSelectedTag(tags[0]);
            return;
        }
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = true;
        NewMode = false;

        SelectedTag = new Session("tag");

        EditedTag = null;

        foreach(Session tag in tags)
        {
            if(EditedTag == null)
            {
                EditedTag = new Session("tag")
                {
                    iFlags = tag.iFlags,
                    Comment = tag.Comment
                };

                SelectedTag.Tags = (tag.Tags != null) ? new ObservableCollection<string>(tag.Tags) : new ObservableCollection<string>();

                _NotInOverviewCheck = (EditedTag.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != 0;
                _NotInOverviewCheckThree = false;

                continue;
            }

            if(NotInOverviewCheck != null && (EditedTag.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW) != (tag.iFlags & ProgramConfig.FLAG_NOTINOVERVIEW))
            {
                _NotInOverviewCheck = null;
                _NotInOverviewCheckThree = true;
            }

            if(tag.Tags != null)
            {
                foreach(string tagName in SelectedTag.Tags.ToList())
                {
                    if(!tag.Tags.Contains(tagName))
                    {
                        SelectedTag.Tags.Remove(tagName);
                    }
                }
            }
            else
            {
                SelectedTag.Tags.Clear();
            }

            if(Comment != "!NoChange!" && Comment != tag.Comment)
            {
                Comment = "!NoChange!";
            }
        }

        CreateTags();

        UpdateGUI();
        HideNotifications();
    }

    public void ShowSelectedTag(Session tag)
    {
        TitleBackground = System.Windows.Application.Current.Resources["bg1"] as SolidColorBrush;
        BatchMode = false;
        NewMode = false;

        EditedTag = LoadSelectedTag(tag);

        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    private Session SelectedTag;
    private Session LoadSelectedTag(Session selectedTag)
    {
        SelectedTag = selectedTag;

        EditedTag = new Session("tag")
        {
            Id = SelectedTag.Id,
            Name = SelectedTag.Name,
            iFlags = SelectedTag.iFlags,
            Comment = SelectedTag.Comment
        };

        return EditedTag;
    }

    public void CreateNewTag()
    {
        TitleBackground = Application.Current.Resources["bg8"] as SolidColorBrush;
        BatchMode = false;
        NewMode = true;
        SelectedTag = null;
        SelectedTags = null;

        EditedTag = new Session("tag");
        CreateTags();
        UpdateGUI();
        HideNotifications();
    }

    public void SaveCurrent()
    {
        OnSaveTag();
    }

    public void HideControl()
    {
        ControlVisible = false;
        NotifyPropertyChanged("ControlVisible");
    }

    private void HideNotifications()
    {
        RemoveError("Name");
    }

    private void UpdateGUI(Visibility controlVisibility = Visibility.Visible)
    {
        ControlVisible = controlVisibility == Visibility.Visible;
        NotifyPropertyChanged("TitleBackground");
        NotifyPropertyChanged("NewMode");
        NotifyPropertyChanged("BatchMode");
        NotifyPropertyChanged("EditMode");
        NotifyPropertyChanged("ControlVisible");
        NotifyPropertyChanged("Name");
        NotifyPropertyChanged("NotInOverviewCheckThree");
        NotifyPropertyChanged("NotInOverviewCheck");
        NotifyPropertyChanged("AssignedTags");
        NotifyPropertyChanged("UnassignedTags");
        NotifyPropertyChanged("SelectedTag2");
        NotifyPropertyChanged("Comment");
        OkValidationVisibility = Visibility.Collapsed;
    }

    private bool NameIsExist(string name)
    {
        return App.Sessions.Sessions.FirstOrDefault((Session s) => s.Name == name && s.Type == "tag" && s.Id != EditedTag.Id) != null;
    }

    private bool InputIsValid()
    {
        if(BatchMode)
        {
            return !HasErrors;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            AddError("Name", "Tag name is requred!");
            return !HasErrors;
        }

        Name = Name.Trim();

        if (NameIsExist(Name))
        {
            if (!NewMode)
            {
                AddError("Name", "Tag name already exists!");
            }
            else
            {
                string text = Name;
                int num = 2;
                while (NameIsExist(Name))
                {
                    text = Name + " (" + num + ")";
                    num++;
                }
                Name = text;
                RemoveError("Name");
            }
        }
        else if (Name == ".." || Name == "...")
        {
            AddError("Name", "Tag name already exists!");
        }
        else
        {
            RemoveError("Name");
        }
        return !HasErrors;
    }

    private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();
    public bool HasErrors => errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public IEnumerable GetErrors(string propertyName)
    {
        if (!errors.ContainsKey(propertyName))
        {
            return null;
        }
        return errors[propertyName];
    }

    protected void ValidateProperty<T>(string propertyName, T value)
    {
        List<ValidationResult> list = new List<ValidationResult>();
        ValidationContext validationContext = new ValidationContext(this) { MemberName = propertyName };
        Validator.TryValidateProperty(value, validationContext, list);
        if (list.Any())
        {
            errors[propertyName] = list.Select((ValidationResult r) => r.ErrorMessage).ToList();
        }
        else
        {
            errors.Remove(propertyName);
        }
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void AddError(string propertyName, string error)
    {
        errors[propertyName] = new List<string> { error };
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected void RemoveError(string propertyName)
    {
        errors.Remove(propertyName);
        this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged(string Property)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Property));
    }
}
