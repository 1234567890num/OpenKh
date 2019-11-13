﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using OpenKh.Common;
using OpenKh.Kh2;
using OpenKh.Kh2.Battle;
using OpenKh.Kh2.Extensions;
using OpenKh.Tools.Common;
using OpenKh.Tools.Kh2BattleEditor.Interfaces;
using Xe.Tools;
using Xe.Tools.Wpf.Commands;
using Xe.Tools.Wpf.Dialogs;

namespace OpenKh.Tools.Kh2BattleEditor.ViewModels
{
    public class MainViewModel : BaseNotifyPropertyChanged
    {
        private static string ApplicationName = Utilities.GetApplicationName();
        private Window Window => Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        private string _fileName;
        private IEnumerable<Bar.Entry> _battleItems;
        private EnmpViewModel _enmp;
        private FmlvViewModel _fmlv;
        private BonsViewModel _bons;
        private PrztViewModel _przt;

        public string Title => $"{FileName ?? "untitled"} | {ApplicationName}";

        private string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public RelayCommand OpenCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand SaveAsCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand AboutCommand { get; }

        public EnmpViewModel Enmp
        {
            get => _enmp;
            private set { _enmp = value; OnPropertyChanged();}
        }

        public FmlvViewModel Fmlv
        {
            get => _fmlv;
            private set { _fmlv = value; OnPropertyChanged(); }
        }

        public BonsViewModel Bons
        {
            get => _bons;
            private set { _bons = value; OnPropertyChanged(); }
        }

        public PrztViewModel Przt
        {
            get => _przt;
            private set { _przt = value; OnPropertyChanged(); }
        }


        public MainViewModel()
        {
            OpenCommand = new RelayCommand(x =>
            {
                var fd = FileDialog.Factory(Window, FileDialog.Behavior.Open, new[]
                {
                    ("00battle.bin", "bin"),
                    ("BAR file", "bar"),
                    ("All files", "*")
                });

                if (fd.ShowDialog() == true)
                {
                    OpenFile(fd.FileName);
                }
            }, x => true);

            SaveCommand = new RelayCommand(x =>
            {
                if (!string.IsNullOrEmpty(FileName))
                {
                    SaveFile(FileName, FileName);
                }
                else
                {
                    SaveAsCommand.Execute(x);
                }
            }, x => true);

            SaveAsCommand = new RelayCommand(x =>
            {
                var fd = FileDialog.Factory(Window, FileDialog.Behavior.Save);
                if (fd.ShowDialog() == true)
                {
                    SaveFile(FileName, fd.FileName);
                    FileName = fd.FileName;
                }
            }, x => true);

            ExitCommand = new RelayCommand(x =>
            {
                Window.Close();
            }, x => true);

            AboutCommand = new RelayCommand(x =>
            {
                new AboutDialog(Assembly.GetExecutingAssembly()).ShowDialog();
            }, x => true);

            CreateBattleItems();
        }

        public bool OpenFile(string fileName) => File.OpenRead(fileName).Using(stream =>
        {
            if (!Bar.IsValid(stream))
            {
                MessageBox.Show(Window, $"{Path.GetFileName(fileName)} is not a valid BAR file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var items = Bar.Read(stream);

            if (!Is00battle(items))
            {
                MessageBox.Show(Window, $"{Path.GetFileName(fileName)} does not appear to be a valid 00battle.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            LoadBattleItems(items);

            FileName = fileName;
            return true;
        });

        public void SaveFile(string previousFileName, string fileName)
        {
            File.Create(fileName).Using(stream =>
            {
                SaveBattleItems();
                Bar.Write(stream, _battleItems);
            });
        }

        private bool Is00battle(List<Bar.Entry> items) => items.Any(x => new[]
        {
            "atkp",
            "enmp",
            "fmlv",
            "lvpm",
            "bons",
            "przt",
        }.Contains(x.Name));

        private void CreateBattleItems()
        {
            _battleItems = new Bar.Entry[0];
            Enmp = GetDefaultBattleViewModelInstance<EnmpViewModel>();
            Fmlv = GetDefaultBattleViewModelInstance<FmlvViewModel>();
            Bons = GetDefaultBattleViewModelInstance<BonsViewModel>();
            Przt = GetDefaultBattleViewModelInstance<PrztViewModel>();
        }

        private void LoadBattleItems(IEnumerable<Bar.Entry> entries)
        {
            _battleItems = entries;
            Enmp = GetBattleViewModelInstance<EnmpViewModel>(_battleItems);
            Fmlv = GetBattleViewModelInstance<FmlvViewModel>(_battleItems);
            Bons = GetBattleViewModelInstance<BonsViewModel>(_battleItems);
            Przt = GetBattleViewModelInstance<PrztViewModel>(_battleItems);
        }

        private void SaveBattleItems()
        {
            _battleItems = SaveBattleItem(_battleItems, Enmp);
            _battleItems = SaveBattleItem(_battleItems, Fmlv);
            _battleItems = SaveBattleItem(_battleItems, Bons);
            _battleItems = SaveBattleItem(_battleItems, Przt);
        }

        private IEnumerable<Bar.Entry> SaveBattleItem(IEnumerable<Bar.Entry> entries, IBattleGetChanges battleGetChanges) =>
            entries.ForEntry(Bar.EntryType.Binary, battleGetChanges.EntryName, 0, entry => entry.Stream = battleGetChanges.CreateStream());

        private T GetBattleViewModelInstance<T>(IEnumerable<Bar.Entry> entries)
            where T : IBattleGetChanges => (T)Activator.CreateInstance(typeof(T), entries);

        private T GetDefaultBattleViewModelInstance<T>()
            where T : IBattleGetChanges => Activator.CreateInstance<T>();
    }
}
