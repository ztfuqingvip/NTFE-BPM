using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Taobao.Workflow.Activities.Hosting;
using System.Diagnostics;
using CodeSharp.Core.Services;
using CodeSharp.Core;

namespace Taobao.Workflow.Activities.Test.Stub
{
    [CodeSharp.Core.Component]
    public class EventBus : IEventBus
    {
        private ILog _log;
        public EventBus(ILoggerFactory factory)
        {
            this._log = factory.Create(typeof(EventBus));
        }

        #region IEventBus Members

        public void RaiseWorkItemArrived(WorkItemArgs args)
        {
            this._log.Debug("RaiseWorkItemArrived");
        }

        public void RaiseHumanActivityInstanceEscalated(ActivityInstanceArgs args)
        {
            this._log.Debug("RaiseHumanActivityInstanceEscalated");
        }

        public void RaiseHumanActivityInstanceStarted(ActivityInstanceArgs args)
        {
            this._log.Debug("RaiseHumanActivityInstanceStarted");
        }

        public void RaiseHumanActivityInstanceCompleted(ActivityInstanceArgs args)
        {
            this._log.Debug("RaiseHumanActivityInstanceCompleted");
        }

        public void RaiseProcessCompleted(ProcessArgs args)
        {
            this._log.Debug("RaiseProcessCompleted");
        }

        #endregion
    }
}