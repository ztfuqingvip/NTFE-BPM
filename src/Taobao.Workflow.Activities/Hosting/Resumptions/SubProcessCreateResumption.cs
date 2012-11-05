/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Services.Transaction;
using CodeSharp.Core;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Hosting
{
    /// <summary>
    /// 子流程待创建请求记录
    /// <remarks>从SubProcessActivityInstance生成Process</remarks>
    /// </summary>
    public class SubProcessCreateResumption : WaitingResumption
    {
        /// <summary>
        /// 获取子流程创建信息
        /// </summary>
        public virtual SubProcessActivityInstance SubProcessActivityInstance { get; private set; }

        protected SubProcessCreateResumption() : base() { }
        /// <summary>
        /// 初始化子流程待创建请求记录
        /// </summary>
        /// <param name="process"></param>
        /// <param name="subProcessActivityInstance"></param>
        public SubProcessCreateResumption(Process process, SubProcessActivityInstance subProcessActivityInstance)
            : base(process, WaitingResumption.MaxPriority)
        {
            this.Init(subProcessActivityInstance);
        }

        public override long? FlowNodeIndex
        {
            get
            {
                return this.SubProcessActivityInstance != null
                    ? this.SubProcessActivityInstance.FlowNodeIndex
                    : _emptyLong;
            }
        }
        public override bool EnableActiveAfterExecuted
        {
            get
            {
                return true;
            }
        }
        public override Type Handle
        {
            get { return typeof(SubProcessCreateWaitingResumption); }
        }

        private void Init(SubProcessActivityInstance subProcessActivityInstance)
        {
            this.SubProcessActivityInstance = subProcessActivityInstance;

            if (this.SubProcessActivityInstance == null)
                throw new InvalidOperationException("SubProcessActivityInstance不能为空");
        }
    }
    /// <summary>
    /// 子流程待创建请求记录处理
    /// </summary>
    [CodeSharp.Core.Component]
    [Transactional]
    public class SubProcessCreateWaitingResumption : IWaitingResumptionHandle
    {
        private ILog _log;
        private IProcessService _processService;
        private ISubProcessHelper _subHelper;
        public SubProcessCreateWaitingResumption(ILoggerFactory factory
            , IProcessService processService
            , ISubProcessHelper subHelper)
        {
            this._log = factory.Create(typeof(SubProcessCreateWaitingResumption));
            this._processService = processService;
            this._subHelper = subHelper;
        }

        #region IWaitingResumption Members
        [Transaction(TransactionMode.Requires)]
        void IWaitingResumptionHandle.Resume(WaitingResumption waitingResumption)
        {
            var r = waitingResumption as SubProcessCreateResumption;
            var sub = r.SubProcessActivityInstance;

            if (sub == null)
            {
                this._log.Warn("不存在待创建子流程的子流程节点信息");
                return;
            }

            var parent = r.Process;
            var setting = parent.ProcessType.GetSubProcessSetting(sub.ActivityName);
            if (setting == null)
                throw new InvalidOperationException(string.Format(
                    "没有找到节点“{0}”对应的SubProcessSetting，可能需要修正流程"
                    , sub.ActivityName));
            //更新子流程节点实例
            var subProcessId = Guid.NewGuid();
            sub.SetSubProcessId(subProcessId);
            this._processService.UpdateActivityInstance(sub);
            //创建子流程
            this._subHelper.Create(parent, setting.SubProcessTypeName, subProcessId);
        }
        #endregion

        /// <summary>
        /// 子流程调度辅助
        /// </summary>
        public interface ISubProcessHelper
        {
            /// <summary>
            /// 创建子流程
            /// </summary>
            /// <param name="parent">父流程</param>
            /// <param name="subProcessTypeName">子流程类型名称</param>
            /// <param name="assignedSubProcessId">可赋值的子流程标识</param>
            /// <returns></returns>
            Process Create(Process parent, string subProcessTypeName, Guid assignedSubProcessId);
        }
        /// <summary>
        /// 默认的子流程调度实现
        /// <remarks>若需要从外围系统发起子流程则派生此类扩展外围数据的生成</remarks>
        /// </summary>
        [Transactional]
        public class DefaultSubProcessHelper : ISubProcessHelper
        {
            private static readonly string _defaultTitle = "由系统启动的子流程";
            private IProcessService _processService;
            private IProcessTypeService _processTypeService;
            private IUserService _userService;
            private string _sysUserName;
            public DefaultSubProcessHelper(IProcessService processService
                , IProcessTypeService processTypeService
                , IUserService userService
                , string systemUserName)
            {
                this._processService = processService;
                this._processTypeService = processTypeService;
                this._userService = userService;
                this._sysUserName = systemUserName;
            }

            #region ISubProcessHelper Members
            [Transaction(TransactionMode.Requires)]
            public virtual Process Create(Process parent, string subProcessTypeName, Guid assignedSubProcessId)
            {
                var subProcessType = this._processTypeService.GetProcessType(subProcessTypeName);
                if (subProcessType == null)
                    throw new InvalidOperationException(string.Format("不存在名称为“{0}”的流程类型信息", subProcessTypeName));

                var subProcess = new Process(string.IsNullOrWhiteSpace(subProcessType.Description)
                    ? _defaultTitle
                    : subProcessType.Description
                    , subProcessType
                    //以系统账号身份发起子流程
                    , this._userService.GetUserWhatever(this._sysUserName)
                    , 10
                    //将父流程变量传递给子流程
                    , parent.GetDataFields()
                    , parent);
                this._processService.Create(subProcess, assignedSubProcessId);
                return subProcess;
            }
            #endregion
        }
    }
}