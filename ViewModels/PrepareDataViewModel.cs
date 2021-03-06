﻿using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLogic;
using BusinessLogic.Clustering;
using BusinessLogic.Exceptions;
using BusinessLogic.Helpers;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ViewModels.Messages;
using ViewModels.Providers;

namespace ViewModels
{
    public class PrepareDataViewModel : ViewModelBase
    {
        private readonly IOpenFileDialogProvider _openFileDialogProvider;
        private readonly IMessageBoxProvider _messageBoxProvider;
        private readonly StringToNumberConverter _stringToNumberConverter;

        private RelayCommand _browseFileCommand;
        private RelayCommand _fillDataCommand;
        private string _contentFileUrl;
        private string _roughSetsFileContent;
        private string _attributesDescription;
        private bool _isBusy;

        public Action GoToAlgorithmChoicePageAction;
        public RelayCommand BrowseFileCommand => _browseFileCommand ?? (_browseFileCommand = new RelayCommand(BrowseFile));
        public RelayCommand FillDataCommand => _fillDataCommand ?? (_fillDataCommand = new RelayCommand(FillData));
        public string DescritpionFileUrl => ContentFileUrl.Insert(ContentFileUrl.Length - 4, "_descr");

        public List<DataObject> DataObjects { get; }

        public List<ClusteredDataObject> ClusteredDataObjects { get; private set; }

        public RoughSetInformations RoughSetInformations { get; private set; }

        public bool CanFillData => !string.IsNullOrEmpty(ContentFileUrl);

        public string ContentFileUrl
        {
            get { return _contentFileUrl; }
            set
            {
                _contentFileUrl = value;
                RaisePropertyChanged(() => CanFillData);
                RaisePropertyChanged(() => ContentFileUrl);
            }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                _isBusy = value; 
                RaisePropertyChanged(() => IsBusy);
            }
        }

        public PrepareDataViewModel(IOpenFileDialogProvider openFileDialogProvider, IMessageBoxProvider messageBoxProvider)
        {
            _openFileDialogProvider = openFileDialogProvider;
            _messageBoxProvider = messageBoxProvider;
            _stringToNumberConverter = new StringToNumberConverter();
            DataObjects = new List<DataObject>();
        }

        private void BrowseFile()
        {
            _openFileDialogProvider.ExecuteOpenFileDialog();
            ContentFileUrl = _openFileDialogProvider.SelectedPath;
        }

        private void FillData()
        {
            try
            {
                IsBusy = true;
                ReadContentFromFiles();
                PrepareRoughSetInformations();
                PrepareDataObjects();
                PrepareClusteredDataObjects();
                SendMessages();
                IsBusy = false;
                GoToAlgorithmChoicePageAction?.Invoke();
            }
            catch (Exception exception)
            {
                IsBusy = false;
                _messageBoxProvider.ShowMessage(exception.Message);
                throw;
            }

        }

        private void PrepareClusteredDataObjects()
        {
            ClusteredDataObjects = ClusteringOperations.Clustering(RoughSetInformations, DataObjects);
        }

        private void SendMessages()
        {
            Messenger.Default.Send(new ClusteredDataObjectsMessage
            {
                ClusteredDataObjects = ClusteredDataObjects
            });
            Messenger.Default.Send(new IndividualLengthMessage
            {
                Length = ClusteredDataObjects[0].Arguments.Count
            });
        }

        private void ReadContentFromFiles()
        {
            _roughSetsFileContent = FileOperations.GetFileContent(ContentFileUrl);
            _attributesDescription = FileOperations.GetFileContent(DescritpionFileUrl);
        }

        private void PrepareRoughSetInformations()
        {
            var stringSeparators = new[] { "\r\n" };
            var lines = _attributesDescription.Substring(0, _attributesDescription.Length - 2).Split(stringSeparators, StringSplitOptions.None);
            var argumentNames = PrepareArgumentNames(lines);
            var decisionClasses = PrepareDecisionClasses(lines);

            CreateRoughSetInformations(argumentNames, decisionClasses);
        }

        private void PrepareDataObjects()
        {
            DataObjects.Clear();
            var stringSeparators = new[] { "\r\n" };
            var lines = _roughSetsFileContent.Substring(0, _roughSetsFileContent.Length - 2).Split(stringSeparators, StringSplitOptions.None);
            _stringToNumberConverter.ConvertStringsToNumbers(lines);
            FillDataObjectsList(lines);
        }

        private void FillDataObjectsList(IEnumerable<string> lines)
        {
            try
            {
                foreach (var argumentsValuesCollection in lines.Select(line => line.Split(',')))
                {
                    DataObjects.Add(new DataObject
                    {
                        Decision = double.Parse(argumentsValuesCollection.Last()),
                        Arguments = argumentsValuesCollection.Take(argumentsValuesCollection.Length - 1)
                            .Select(l => double.Parse(l.Replace('.', ','))).ToList()
                    });
                }
            }
            catch (Exception exception)
            {
                throw new FillDataObjectsListException(exception);
            }
        }

        private static List<string> PrepareArgumentNames(IReadOnlyList<string> lines)
        {
            var argumentsNames = new List<string>();

            for (var i = 1; i < lines.Count; i++)
                argumentsNames.Add(lines[i].Split(':', ',')[0]);

            return argumentsNames;
        }

        private static List<string> PrepareDecisionClasses(IReadOnlyList<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return new List<string>();

            return lines[0].Split(',').ToList();
        }

        private void CreateRoughSetInformations(List<string> argumentNames, List<string> decisionClasses)
        {
            RoughSetInformations = new RoughSetInformations(argumentNames, decisionClasses);
        }
    }
}
