using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using UIAutomationClient;

namespace UIAutomation
{
    public class HookMessageProcess
    {
        //메신저의 핸들값을 가져오기 위한 CaptionName;
        private readonly string messengerCaptionNmae = "chatBrowser";

        private IUIAutomation automation;
        private ListView listViewMsg;
        //메신저내용을 다시 가져오는걸 막기위한 변수
        private bool refreshInProgress = false;

        //메세지 후킹쓰레드(MTA thread)
        private Thread threadHookMessage;

        //메신저의 메시지후킹 쓰레드(threadHookMessage) 특정 액션을 전달 하기위한 이벤트
        private AutoResetEvent autoEventMsg;
        //메신저의 메시지후킹 쓰레드(threadHookMessage) 특정 액션을 전달받기 위한 액션 초기화 이벤트
        private AutoResetEvent autoEventInit;


        //이벤트 MSG타입
        private enum EventMsgType
        {
            msgNull,
            msgMessageBulidList,
            //msgHighlightLink,
            //msgInvodeLink,
            msgCloseDown
        }

        //이벤트 MSG DATA
        private struct EventMsgData
        {
            public EventMsgType msgType;
            public AutomationElement element;
            public bool useCache;
        }

        //이벤트 순차처리 하기 위해 큐생성
        private Queue<EventMsgData> msgQueue = new Queue<EventMsgData>();


        //리스트박스에 표시될 MSG
        public class MessageItem
        {
            public string msgValue;
            public object element;
        }

        private List<MessageItem> messageItems = new List<MessageItem>();

     
        //받아온 메시지를 메인 쓰레드에 보내기 위한 Delegate 
        UIListUpdateDelegate uIListUpdateDelegate;

        // Note that UIAutomationClient.UIA_PatternIds.UIA_InvokePatternId could be 
        // supplied to calls to UIA in this file, but that would require building the 
        // sample using settings other than the VS defaults. So instead supply a value 
        // equal to UIAutomationClient.UIA_PatternIds.UIA_InvokePatternId;
        private int _patternIdInvoke = 10000;

        // Similarly for other ids.
        private int _propertyIdBoundingRectangle = 30001;
        private int _propertyIdControlType = 30003;
        private int _propertyIdName = 30005;
        private int _controlTypeIdHyperlink = 50005;
        private int _controlTypeIdMessengerMessage = 50033;
        private int _patternIdValue = 10002;

        //메세지 후킹 작업시작을 위한 쓰레드 시작
        public void Initialize(UIListUpdateDelegate uIUpdateDelegate, ListView listView)
        {
            uIListUpdateDelegate = uIUpdateDelegate;
            listViewMsg = listView;

            if (threadHookMessage != null)
            {
                return;
            }

            //메시지후킹 쓰레드에 특정액션이 발생하기 위함
            autoEventMsg = new AutoResetEvent(false);

            //메시지후킹 쓰레드를 만들고 프로세스가 실행될 때까지 기다림
            autoEventInit = new AutoResetEvent(false);
            ParameterizedThreadStart paramThreadStart = new ParameterizedThreadStart(DoWork);

            threadHookMessage = new Thread(paramThreadStart);
            threadHookMessage.SetApartmentState(ApartmentState.MTA);
            threadHookMessage.Start(this);

            autoEventInit.WaitOne();
        }

        //메세지 후킹 작업종료
        public void Uninitialize()
        {
            // 메시지 후킹쓰레드를 종료하기 위한 Msg큐 저장
            if (threadHookMessage != null)
            {
                EventMsgData msgData = new EventMsgData();
                msgData.msgType = EventMsgType.msgCloseDown;
                AddMsgToQueue(msgData);
            }
        }       


        public void BuildListOfMsgFromWindow(bool useCache, bool searchChildren)
        {
            EventMsgData msgData = new EventMsgData();
            msgData.msgType = EventMsgType.msgMessageBulidList;
            AddMsgToQueue(msgData);
        }

        private void AddMsgToQueue(EventMsgData msgData)
        {
            // 요청에대한 Lock 처리
            Monitor.Enter(msgQueue);
            try
            {
                // LOCK 처리 후 해당msg 큐에삽입
                msgQueue.Enqueue(msgData);
            }
            finally
            {
                // Lock 해제
                Monitor.Exit(msgQueue);
            }

            // 큐에 삽인된 MSG처리 요청
            autoEventMsg.Set();
        }

        //메시지 후킹작업 시작
        private static void DoWork(object data)
        {
            HookMessageProcess hookMsgProcessor = (HookMessageProcess)data;
            //hookMsgProcessor.ThreadProc();
        }

        private void ThreadProc()
        {
            // *** Note: The thread on which the UIA calls are made below must be MTA.
            automation = new CUIAutomation();

            autoEventInit.Set();

            
            bool closeDown = false;

            while (!closeDown)
            {
                // Wait here until we're told we have some work to do.
                autoEventMsg.WaitOne();

                while (true)
                {
                    EventMsgData msgData;

                    // Note that none of the queue or message related action here is specific to UIA.
                    // Rather it is only a means for the main UI thread and the background MTA thread
                    // to communicate.

                    // Get a message from the queue of action-related messages.
                    Monitor.Enter(msgQueue);
                    try
                    {
                        // An exception is thrown when the queue is empty.
                        msgData = msgQueue.Dequeue();
                    }
                    catch (InvalidOperationException)
                    {
                        // InvalidOperationException is thrown if the queue is empty.
                        msgData.msgType = EventMsgType.msgNull;
                        msgData.element = null;
                        msgData.useCache = false;

                        break;
                    }
                    finally
                    {
                        // Ensure that the lock is released.
                        Monitor.Exit(msgQueue);
                    }

                    if (msgData.msgType == EventMsgType.msgMessageBulidList)
                    {
                        // We will be here following a press of the Refresh button by the user.
                        BuildListOfMsgFromWindowInternal(false, false);
                    }                   
                    else if (msgData.msgType == EventMsgType.msgCloseDown)
                    {
                        //메인쓰레드 에서 메세지 후킹 쓰레드 종료 Msg 처리.
                        closeDown = true;

                        break;
                    }
                }
            }
        }


        
        private void BuildListOfMsgFromWindowInternal(bool useCache, bool searchLinkChildren)
        {
            //메인쓰레드에어 메시지 가져오기 요청을 이미 진행 했으면 처리하지 않음
            if (refreshInProgress)
            {
                return;
            }

            refreshInProgress = true;

            // 메신저의 윈도우 핸들을 가져옴
            //IntPtr hwnd = Win32.FindWindow(null, "messengerCaptionNmae");

            IntPtr hwnd = new IntPtr(8719308);
            if (hwnd != IntPtr.Zero)
            {
                // Get a UIAutomationElement associated with this browser window. Note that the IUIAutomation 
                // interface has other useful functions for retrieving automation elements. For example:
                //
                //   ElementFromPoint() - convenient when getting the UIA element beneath the mouse cursor.
                //   GetFocusedElement() - convenient when you need the UIA element for whatever control 
                //                         currently has keyboard focus.
                //
                // All these functions have cache-related equivalents which can reduce the 
                // time it takes to work with the element once it's been retrieved.



                IUIAutomationElement element = automation.ElementFromHandle(hwnd);
                if (element != null)
                {
                    // If all we needed to do is get a few properties of the element we now have, we could 
                    // make the get_Current* calls such shown below.) But these would incur the time cost of 
                    // additional cross-proc calls.)

                    string strName = element.CurrentName;
                    // Do something with the name...

                    tagRECT rect = element.CurrentBoundingRectangle;
                    // Do something with the bounding rect...

                    // Rather than doing the above, a shipping app might choose to request the name
                    // and bounding rect of the browser when it retrieves the browser element. It
                    // could do this by calling ElementFromHandleBuildCache() supplying a cache
                    // request which included the properties it needs. By doing that, there would 
                    // only be one cross-proc call rather than the three involved with the above steps.

                    // For this sample, we'll build up a list of all hyperlinks in the browser window.
                    BuildListOfMsgFromElement(element, useCache, searchLinkChildren);
                }
            }

            // Allow another refresh to be performed now.
            refreshInProgress = false;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // BuildListOfHyperlinksFromElement()
        //
        // Retrieve a list of hyperlinks from a UIAutomation element. Notifies the main 
        // UI thread when it's found all the hyperlinks to be added to the app's list of 
        // hyperlinks.
        //
        // Runs on the background thread.
        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void BuildListOfMsgFromElement(IUIAutomationElement elementBrowser,
                                                      bool fUseCache, bool fSearchLinkChildren)
        {
            IUIAutomationCacheRequest cacheRequest = null;

            // If a cache is used, then for each of the elements returned to us after a search 
            // for elements, specific properties (and patterns), can be cached with the element. 
            // This means that when we access one of the properties later, a cross-proc call 
            // does not have to be made. (But it also means that when such a call is made, we
            // don't learn whether the original element still exists.)
            if (fUseCache)
            {
                // Create a cache request containing all the properties and patterns
                // we will need once we've retrieved the hyperlinks. By using this
                // cache, when can avoid time-consuming cross-process calls when
                // getting hyperlink properties later.
                cacheRequest = automation.CreateCacheRequest();

                // We'll need the hyperlink's name and bounding rectangle later.
                // A full list of Automation element properties can be found at 
                // http://msdn.microsoft.com/en-us/library/ee684017(v=VS.85).aspx.

                cacheRequest.AddProperty(_propertyIdName);
                cacheRequest.AddProperty(_propertyIdBoundingRectangle);

                // The target of the hyperlink might be stored in the Value property of 
                // the hyperlink. The Value property is only avaliable if an element
                // supports the Value pattern. This sample doesn't use the Value, but
                // if it did, it would call the following here.
                //  hr = pCacheRequest->AddProperty(UIA_ValueValuePropertyId);
                // It's ok to attempt to cache a property on a pattern which might not 
                // exist on the cached elements. In that case, the property just won't
                // be available when we try to retrieve it from the cache later.

                // Note that calling AddPattern() does not cache the properties 
                // associated with a pattern. The pattern's properties must be
                // added explicitly to the cache if required.

                // Cache the Invoke pattern too. This means when we prepare to invoke a link later,
                // we won't need to make a cross-proc call during that preparation. (A cross-proc
                // call will occur at the time Invoke() is actually called.) A full list of patterns
                // can be found at http://msdn.microsoft.com/en-us/library/ee684023(v=VS.85).aspx.

                cacheRequest.AddPattern(_patternIdInvoke);

                // The next step is to specify for which elements we want to have the properties, (and 
                // pattern) cached. By default, caching will occur on each element found in the search 
                // below. But we can also request that the data is  cached for direct children of the
                // elements found, or even all the descendants of the elements founds. (A scope of 
                // Parent or Ancestors cannot be used in a cached request.)

                // So in this sample, if TreeScope_Element is used as the scope here, (which is the 
                // default), then only properties for the found hyperlinks will be cached. The sample
                // optionally caches the properties for the direct children of the hyperlinks too. 
                // This means that if we find a hyperlink with no name, we can search the hyperlink's
                // children to see if one of the child elements has a name we can use. (Searching the 
                // children could be done without using the cache, but it would incur the time cost of
                // making cross-proc calls.) 

                TreeScope scope = TreeScope.TreeScope_Element;

                if (fSearchLinkChildren)
                {
                    scope = scope | TreeScope.TreeScope_Children;
                }

                cacheRequest.TreeScope = scope;

                // Note: By default the cache request has a Mode of Full. This means a reference to the 
                // target element is included in the cache, as well as whatever properties and patterns
                // we specified should be in the cache. With a reference to the target element, we can:
                //
                // (i) Retrieve a property later for an element which we didn't request should be in 
                //     the cache. Eg we could call get_CurrentHasKeyboardFocus().
                //
                // (ii) We can call a method of a pattern that the element supports. Eg if Full mode was
                //      not used here, we would not be able to call Invoke() on the hyperlink later.

                // If we specified a Mode of None for the cache request here, then the results only include
                // cached data, with no connection at all after the call returns to the source elements. If 
                // only data is required, then it would be preference to use a Mode of None, as less work is 
                // required by UIA. (Also, if a reference to the element is returned in the cache and kept 
                // around for a non-trivial time, then it increases the chances that the target process 
                // attempts to free the element, but it can't do so in a clean manner as it would like, 
                // due to the client app here holding a reference to it.) To specify that we want a Mode of 
                // None, we'd make this call here:

                // cacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;
            }

            // Now regardless of whether we're using a cache, we need to specify which elements
            // we're interested in during our search for elements. We do this by building up a
            // property condition. This property condition tells UIA which properties must be 
            // satisfied by an element for it to be included in the search results. We can 
            // combine a number of properties with AND and OR logic.

            // We shall first say that we're only interested in elements that exist in the Control view. 
            // By default, a property condition uses the Raw view, which means that every element in the 
            // target browser's UIA tree will be examined. The Control view is a subset of the Raw view, 
            // and only includes elements which present some useful UI. (The Raw view might include
            // container elements which simply group elements logically together, but the containers 
            // themselves might have no visual representation on the screen.)

            IUIAutomationCondition conditionControlView = automation.ControlViewCondition;
            //IUIAutomationCondition conditionHyperlink = automation.CreatePropertyCondition(_propertyIdControlType, _controlTypeIdHyperlink);
            IUIAutomationCondition conditionMessengerMsg = automation.CreatePropertyCondition(_propertyIdControlType, _controlTypeIdMessengerMessage);

            // Now combine these two properties such that the search results only contain
            // elements that are in the Control view AND are hyperlinks. We would get the
            // same results here if we didn't include the Control view clause, (as hyperlinks
            // won't exist only in the Raw view), but by specifying that we're only interested
            // in the Control view, UIA won't bother checking all the elements that only exist
            // in the Raw view to see if they're hyperlinks.
            IUIAutomationCondition condition = automation.CreateAndCondition(conditionControlView, conditionMessengerMsg);

            // Now retrieve all the hyperlinks in the browser. We must specify a scope in the Find calls here,
            // to control how far UIA will go in looking for elements to include in the search results. For
            // this sample, we must check all descendants of the browser window. 

            // *** Note: use TreeScope_Descendants with care, as depending on what you're searching for, UIA may
            // have to process potentially thousands of elements. For example, if you only need to find top level 
            // windows on your desktop, you would search for TreeScope_Children of the root of the UIA tree. (The 
            // root element can be found with a call to IUIAutomation::GetRootElement().)

            // *** Note: If the following searches included searching for elements in the client app's own UI,
            // then the calls must be made on a background thread. (ie not your main UI thread.) Once event
            // handlers are used, then it's preferable to have all UIA calls made on a background thread
            // regardless of whether the app interacts with its own UI.

            IUIAutomationElementArray elementArray;

            if (fUseCache)
            {
                elementArray = elementBrowser.FindAllBuildCache(TreeScope.TreeScope_Descendants, condition, cacheRequest);
            }
            else
            {
                elementArray = elementBrowser.FindAll(TreeScope.TreeScope_Descendants, condition);
            }

            // Build up a list of items to be passed to the main thread in order for it to
            // populate the list of hyperlinks shown in the UI.

            messageItems.Clear();

            if (elementArray != null)
            {
                // Find the number of hyperlinks returned by the search. (The number of hyperlinks 
                // found might be zero if the browser window is minimized.)
                int cLinks = elementArray.Length;

                // Process each returned hyperlink element.
                for (int idxLink = 0; idxLink < cLinks; ++idxLink)
                {
                    IUIAutomationElement elementLink = elementArray.GetElement(idxLink);

                    // Get the name property for the hyperlink element. How we get this depends
                    // on whether we requested that the property should be cached or not.

                    string strLinkName = null;

                    if (fUseCache)
                    {
                        strLinkName = GetCachedDataFromElement(elementLink, fSearchLinkChildren);
                    }
                    else
                    {
                        strLinkName = GetCurrentDataFromElement(elementLink, fSearchLinkChildren);
                    }

                    // If we have non-null name, add the link to the list. (This sample does not check
                    // for names that only contains whitespace.)
                    if (strLinkName != null)
                    {
                        strLinkName = strLinkName.Trim();

                        MessageItem item = new MessageItem();
                        item.msgValue = strLinkName;
                        item.element = elementLink;

                        messageItems.Add(item);
                    }
                }
            }

            // Notify the main UI thread that a list of links is ready for processing. Do not block in this call.
            listViewMsg.BeginInvoke(uIListUpdateDelegate, messageItems);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // GetCachedDataFromElement()
        //
        // Get the cached name from a UIA element. If the element doesn't have a name, 
        // optionally try to find a name from the cached children of the element.
        //
        // Runs on the background thread.
        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private string GetCachedDataFromElement(IUIAutomationElement element, bool fSearchLinkChildren)
        {
            // (A shipping app would do parameter checking here.)

            string strName = null;

            // Get the bounding rectangle for the hyperlink. By retrieving this from the 
            // cache, we avoid the time-consuming cross-process call to get the data. 
            tagRECT rectBounds = element.CachedBoundingRectangle;

            // If the hyperlink has a zero-size bounding rect, ignore the element. This 
            // might happen if the hyperlink has scrolled out of view. (We could also 
            // investigate whether using the IsOffscreen property tells us that the link 
            // can be ignored. In fact, if the IsOffscreen property is reliable, we could 
            // have included a property condition of IsOffcreen is false in the original 
            // search, and not check whether the link's visible here.)
            if ((rectBounds.right > rectBounds.left) && (rectBounds.bottom > rectBounds.top))
            {
                // Get the name of the element. This will often be the text shown on the screen. 
                // Note that the set of get_Cached* functions (or get_Current*), are convenient 
                // ways of retrieving the same data that could be retrieved through the functions 
                // GetCachedPropertyValue() (or GetCurrentPropertValue().) In the case of get_CachedName(), 
                // the alternative would be to call GetCachedPropertyValue() with UIA_NamePropertyId.
                string strNameFound = element.CachedName;
                if (strNameFound.Length > 0)
                {
                    // A shipping app would check for more than an empty string. (A link might
                    // just have " " for a name.)
                    strName = strNameFound;
                }
                else
                {
                    // The hyperlink has no usable name. Consider using the name of a child element of the hyperlink.
                    if (fSearchLinkChildren)
                    {
                        // Given that hyperlink element has no name, use the name of first child 
                        // element that does have a name. (In some cases the hyperlink element might 
                        // contain an image or text element that does have a useful name.) We can take 
                        // this action here because we supplied TreeScope_Children as the scope of the 
                        // cache request that we passed in the call to FindAllBuildCache().

                        IUIAutomationElementArray elementArrayChildren = element.GetCachedChildren();
                        if (elementArrayChildren != null)
                        {
                            int cChildren = elementArrayChildren.Length;

                            // For each child element found...
                            for (int idxChild = 0; idxChild < cChildren; ++idxChild)
                            {
                                IUIAutomationElement elementChild = elementArrayChildren.GetElement(idxChild);
                                if (elementChild != null)
                                {
                                    string strNameChild = elementChild.CachedName;

                                    // Check the name of the child elements here. We don't 
                                    // care what type of element it is in this sample app.
                                    if (strNameChild.Length > 0)
                                    {
                                        // We have a usable name.
                                        strName = strNameChild;
                                        break;
                                    }

                                    // Try the next child element of the hyperlink...
                                }
                            }
                        }
                    }
                }
            }

            return strName;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //
        // GetCurrentDataFromElement()
        //
        // Get the current name from a UIA element, (incurring the time cost of various a cross-proc calls). 
        // If the element doesn't have a name, optionally try to find a name from the children of the element
        // by using a TreeWalker.
        //
        // Runs on the background thread.
        //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private string GetCurrentDataFromElement(IUIAutomationElement element, bool fSearchLinkChildren)
        {
            string strName = null;

            // Call back to the target app to retrieve the bounds of the hyperlink element.
            tagRECT rectBounds = element.CurrentBoundingRectangle;

            // If we're the hyperlink has a zero-size bounding rect, ignore the element.
            if ((rectBounds.right > rectBounds.left) && (rectBounds.bottom > rectBounds.top))
            {
                // Get the name of the element, (again incurring a cross-proc call). This name will often 
                // be the text shown on the screen.
                string strNameFound = element.CurrentName;
                if ((strNameFound != null) && (strNameFound.Length > 0))
                {
                    // We have a usable name.
                    strName = strNameFound;
                }
                else
                {
                    // The hyperlink has no usable name. Consider using the name of a child element of the hyperlink.
                    if (fSearchLinkChildren)
                    {
                        // Use a Tree Walker here to try to find a child element of the hyperlink 
                        // that has a name. Tree walking is a time consuming action, so would be
                        // avoided by a shipping app if alternatives like FindFirst, FindAll, or 
                        // BuildUpdatedCache could get the required data.

                        IUIAutomationTreeWalker controlWalker = automation.ControlViewWalker;

                        IUIAutomationElement elementChild = controlWalker.GetFirstChildElement(element);
                        while (elementChild != null)
                        {
                            string strNameChild = elementChild.CurrentName;
                            if ((strNameChild != null) && (strNameChild.Length > 0))
                            {
                                // Use the name of this child element.
                                strName = strNameChild;
                                break;
                            }

                            // Continue to the next child element.
                            elementChild = controlWalker.GetNextSiblingElement(elementChild);
                        }
                    }
                }
            }

            // While this sample doesn't use the destination of the hyperlink, if it wanted 
            // to get the destination, that might be available as the Value property of the 
            // element. The Value property is part of the Value pattern, and so is accessed
            // through the Value pattern.

            // Check first whether the element supports the Value pattern.
            IUIAutomationValuePattern valuePattern = (IUIAutomationValuePattern)element.GetCurrentPattern(_patternIdValue);
            if (valuePattern != null)
            {
                string strValue = valuePattern.CurrentValue;

                // This is where the destination of the link would be used...
            }

            return strName;
        }


    }
}
