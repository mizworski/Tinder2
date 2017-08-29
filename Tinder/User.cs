using System;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using Interface;
using Tinder.Pages;

namespace Tinder
{
    [CallbackBehavior(UseSynchronizationContext = false)]
    public class User : IUser
    {
        private readonly ListBox _pairs;
        private readonly TextBlock _chat;
        private readonly ScrollViewer _scroller;
        private readonly Chat _ui;

        public User()
        {
            _pairs = null;
            _chat = null;
            _scroller = null;
        }

        public User(Chat ui, ListBox pairs, TextBlock chat, ScrollViewer scroller)
        {
            _ui = ui;
            _pairs = pairs;
            _chat = chat;
            _scroller = scroller;
        }

        public void LoadMessages()
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                _ui.RefreshChat(_pairs, _chat, _scroller);
            }));
        }
    }
}
