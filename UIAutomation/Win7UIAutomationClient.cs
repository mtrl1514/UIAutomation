using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UIAutomation
{
    public delegate void UIListUpdateDelegate(object itemList);

    public partial class Win7UIAutomationClient : Form
    {
        private HookMessageProcess hookMsgProcessor;

        public Win7UIAutomationClient()
        {
            InitializeComponent();
            InitializeHookMsg();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            PopulateListOfLinks();
        }

        void InitializeHookMsg()
        {
            // Initialize the object which will make all the UIA calls on a background MTA thread.
            hookMsgProcessor = new HookMessageProcess();
            hookMsgProcessor.Initialize(new UIListUpdateDelegate(UpdateListOnUIThread), listViewLinks);

            // Initialize the event handler which is called when the UIA tree structure changes in the browser window.
            //SampleUIEventHandlerDelegate sampleUIEventHandlerDelegate = new SampleUIEventHandlerDelegate(HandleEventOnUIThread);
            //_sampleEventHandler = new SampleEventHandler(this, sampleUIEventHandlerDelegate);
        }

        private void UpdateListOnUIThread(object itemList)
        {
            // The LinkProcessor provides us with a list of items which contain
            // the name to be shown for the hyperlink, and a reference to the 
            // UIA element representing the hyperlink.

            listViewLinks.BeginUpdate();
            listViewLinks.Items.Clear();

            foreach (HookMessageProcess.MessageItem item in (List<HookMessageProcess.MessageItem>)itemList)
            {
                ListViewItem listviewitem = listViewLinks.Items.Add(item.msgValue);
                listviewitem.Tag = item.element;
            }

            listViewLinks.EndUpdate();
            listViewLinks.Refresh();

            // Show the number of hyperlinks in a label beneath the list.
            labelMsgCountValue.Text = listViewLinks.Items.Count.ToString();
        }

        private void PopulateListOfLinks()
        {


            bool fUseCache = false;
            bool fSearchLinkChildren = false;



            hookMsgProcessor.BuildListOfMsgFromWindow(fUseCache, fSearchLinkChildren);
        }

        private void Win7UIAutomationClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (hookMsgProcessor != null)
            {
                hookMsgProcessor.Uninitialize();
                hookMsgProcessor = null;
            }

          
            //if (_sampleEventHandler != null)
            //{
            //    _sampleEventHandler.Uninitialize();
            //    _sampleEventHandler = null;
            //}

            // Give any background threads created on startup a chance to close down gracefully.
            Thread.Sleep(200);
        }
    }
}
