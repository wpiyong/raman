using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModelLib;

namespace RamanMapping.ViewModel
{
    public class StatusMessage
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    class StatusViewModel : ViewModelBase
    {

        #region Properties

        ObservableCollection<StatusMessage> _camMessages = new ObservableCollection<StatusMessage>();
        public ObservableCollection<StatusMessage> CamMessages
        {
            get { return _camMessages; }
        }

        void _camMessages_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("CamCurrentMessage");
        }

        public string CamCurrentMessage
        {
            get
            {
                if (_camMessages.Count > 0)
                {
                    return _camMessages.OrderBy(p => p.Timestamp).Last().Message;
                }
                else
                    return string.Empty;
            }
        }

        ObservableCollection<StatusMessage> _motoMessages = new ObservableCollection<StatusMessage>();
        public ObservableCollection<StatusMessage> MotorMessages
        {
            get { return _motoMessages; }
        }

        void _motoMessages_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("MotoCurrentMessage");
        }

        public string MotoCurrentMessage
        {
            get
            {
                if (_motoMessages.Count > 0)
                {
                    return _motoMessages.OrderBy(p => p.Timestamp).Last().Message;
                }
                else
                    return string.Empty;
            }
        }

        ObservableCollection<StatusMessage> _specMessages = new ObservableCollection<StatusMessage>();
        public ObservableCollection<StatusMessage> SpecMessages
        {
            get { return _specMessages; }
        }

        void _specMessages_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("SpecCurrentMessage");
        }

        public string SpecCurrentMessage
        {
            get
            {
                if (_specMessages.Count > 0)
                {
                    return _specMessages.OrderBy(p => p.Timestamp).Last().Message;
                }
                else
                    return string.Empty;
            }
        }

        byte _busy;
        public byte Busy
        {
            get { return _busy; }
            set
            {
                _busy = value;
                OnPropertyChanged("Busy");
            }
        }

        bool _isBusy;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if(_isBusy == value)
                {
                    return;
                }
                _isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        int _currentProgress;
        public int CurrentProgress
        {
            get
            {
                return _currentProgress;
            }
            set
            {
                if(_currentProgress == value)
                {
                    return;
                }
                _currentProgress = value;
                OnPropertyChanged("CurrentProgress");
            }
        }

        #endregion

        public StatusViewModel()
        {
            base.DisplayName = "StatusViewModel";
            _camMessages.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_camMessages_CollectionChanged);
            _motoMessages.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_motoMessages_CollectionChanged);
            _specMessages.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(_specMessages_CollectionChanged);
        }

    }
}
