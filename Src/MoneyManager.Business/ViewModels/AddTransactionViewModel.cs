﻿using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using MoneyManager.Business.Logic;
using MoneyManager.Business.Manager;
using MoneyManager.DataAccess.DataAccess;
using MoneyManager.Foundation;
using MoneyManager.Foundation.Model;
using MoneyManager.Foundation.OperationContracts;
using PropertyChanged;

namespace MoneyManager.Business.ViewModels
{
    [ImplementPropertyChanged]
    public class AddTransactionViewModel : ViewModelBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly CurrencyManager _currencyManager;
        private readonly SettingDataAccess _settings;
        private readonly ITransactionRepository _transactionRepository;

        public AddTransactionViewModel(ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            CurrencyManager currencyManager,
            SettingDataAccess settings)
        {
            _transactionRepository = transactionRepository;
            _currencyManager = currencyManager;
            _settings = settings;
            _accountRepository = accountRepository;
            
            IsNavigationBlocked = true;
        }

        public bool IsNavigationBlocked { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsEndless { get; set; }
        public bool IsEdit { get; set; }
        public int Recurrence { get; set; }
        public bool IsTransfer { get; set; }
        public bool RefreshRealtedList { get; set; }

        public FinancialTransaction SelectedTransaction
        {
            get { return _transactionRepository.Selected; }
            set { _transactionRepository.Selected = value; }
        }

        public string DefaultCurrency => _settings.DefaultCurrency;

        public ObservableCollection<Account> AllAccounts => _accountRepository.Data;

        public string Title
        {
            get
            {
                var text = IsEdit
                    ? Translation.GetTranslation("EditTitle")
                    : Translation.GetTranslation("AddTitle");

                var type = TransactionTypeLogic.GetViewTitleForType(_transactionRepository.Selected.Type);

                return string.Format(text, type);
            }
        }

        public DateTime Date
        {
            get
            {
                if (!IsEdit)
                {
                    SelectedTransaction.Date = DateTime.Now;
                }
                return SelectedTransaction.Date;
            }
            set { SelectedTransaction.Date = value; }
        }

        public string AmountString
        {
            get { return AmountWithoutExchange.ToString(); }
            set
            {
                double amount;
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentUICulture, out amount))
                {
                    AmountWithoutExchange = amount;
                }
            }
        }

        private double AmountWithoutExchange
        {
            get { return _transactionRepository.Selected.AmountWithoutExchange; }
            set
            {
                _transactionRepository.Selected.AmountWithoutExchange = value;
                CalculateNewAmount(value);
            }
        }

        private void CalculateNewAmount(double value)
        {
            if (Math.Abs(_transactionRepository.Selected.ExchangeRatio) < 0.5)
            {
                _transactionRepository.Selected.ExchangeRatio = 1;
            }

            _transactionRepository.Selected.Amount = _transactionRepository.Selected.ExchangeRatio*value;
        }

        public async void SetCurrency(string currency)
        {
            _transactionRepository.Selected.Currency = currency;
            await LoadCurrencyRatio();
            _transactionRepository.Selected.IsExchangeModeActive = true;
            CalculateNewAmount(AmountWithoutExchange);
        }

        private async Task LoadCurrencyRatio()
        {
            _transactionRepository.Selected.ExchangeRatio =
                await
                    _currencyManager.GetCurrencyRatio(_settings.DefaultCurrency,
                        _transactionRepository.Selected.Currency);
        }

        public async void Save()
        {
            if (_transactionRepository.Selected.ChargedAccount == null)
            {
                ShowAccountRequiredMessage();
                return;
            }

            if (IsEdit)
            {
                await TransactionLogic.UpdateTransaction(_transactionRepository.Selected);
            } else
            {
                await TransactionLogic.SaveTransaction(_transactionRepository.Selected, RefreshRealtedList);
            }

            ((Frame) Window.Current.Content).GoBack();
        }

        private async void ShowAccountRequiredMessage()
        {
            var dialog = new MessageDialog
                (
                Translation.GetTranslation("AccountRequiredMessage"),
                Translation.GetTranslation("MandatoryField")
                );
            dialog.Commands.Add(new UICommand(Translation.GetTranslation("OkLabel")));
            dialog.DefaultCommandIndex = 1;
            await dialog.ShowAsync();
        }

        public async void Cancel()
        {
            if (IsEdit)
            {
                await AccountLogic.AddTransactionAmount(_transactionRepository.Selected);
            }
            ((Frame) Window.Current.Content).GoBack();
        }
    }
}