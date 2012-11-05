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
using System.Linq.Expressions;

using Taobao.Activities;
using Taobao.Activities.Expressions;
using Taobao.Activities.Statements;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 自定义活动/节点
    /// <remarks>
    /// 将用于描述常用工作流 人工/用户任务等环节
    /// 将存在于flowchart的flowstep中
    /// </remarks>
    /// </summary>
    public abstract class Custom : NativeActivity, Flowchart.IMetadataKnowable
    {
        private static readonly string _split = "#";
        private static readonly string _errorBookmarkPrefix = "Error#";
        private static readonly DateTime? _emptyTime = new DateTime?();
        private static readonly int _defaultIndex = WorkflowBuilder.Default_FlowNodeIndex;
        /// <summary>
        /// 获取或设置FlowNodeIndex被设置时的回调
        /// </summary>
        public Action<int> OnFlowNodeIndex { get; set; }
        //获取所在flowchart中的索引
        protected int FlowNodeIndex { get; private set; }

        /// <summary>
        /// 获取获取设置任务执行结果输出参数
        /// </summary>
        public OutArgument<string> Result { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public Custom() : this(_defaultIndex) { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        public Custom(int flowNodeIndex)
        {
            this.FlowNodeIndex = flowNodeIndex;
            this.Result = new OutArgument<string>();
        }

        sealed protected override void Execute(NativeActivityContext context)
        {
            //HACK:节点执行时总是首先设置当前节点索引
            context.GetExtension<DataFieldExtension>().SetCurrentNode(this.FlowNodeIndex);

            var e = context.GetExtension<CustomExtension>();
            var setting = e.GetActivitySetting(this.DisplayName);

            if (setting == null)
            {
                this.InternalExecuteBody(context);
                return;
            }

            var at = setting.StartRule != null && setting.StartRule.IsValid
                ? setting.StartRule.GetStartTime()
                : _emptyTime;
            //触发节点开始规则，创建延迟书签
            if (at.HasValue)
                e.AddDelay(this.DisplayName
                    , context.CreateBookmark(this.GenerateBookmarkName(context)
                    , this.OnDelayBookmarkCallback).Name
                    , at.Value);
            else
                this.InternalExecuteBody(context);
        }
        //延迟书签回调
        private void OnDelayBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            context.RemoveBookmark(bookmark);
            //删除延迟书签后直接执行主体
            this.InternalExecuteBody(context);
        }
        //错误书签回调
        private void OnFaultBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            context.RemoveBookmark(bookmark);
            //删除错误书签后直接执行主体
            this.InternalExecuteBody(context);
        }
        //执行实际节点的主体 包含异常书签处理
        private void InternalExecuteBody(NativeActivityContext context)
        {
            this.ExecuteAndDealWithError(() => this.ExecuteBody(context), context);
            //若在此处直接抛异常将直接上升为工作流实例异常
        }

        protected void ExecuteAndDealWithError(Action func, NativeActivityContext context)
        {
            this.ExecuteAndDealWithError(func, context, null);
        }
        protected void ExecuteAndDealWithError(Action func, NativeActivityContext context, Bookmark faultBookmark)
        {
            var extension = context.GetExtension<CustomExtension>();
            try
            {
                func();
            }
            catch (Exception reason)
            {
                //利用书签来进行细粒度错误恢复而不提升为工作流实例异常
                extension.AddFault(this.DisplayName
                    //错误书签可复用原有书签或新建
                    , (faultBookmark ?? context.CreateBookmark(this.GenerateErrorBookmarkName(context), this.OnFaultBookmarkCallback)).Name
                    , reason);
            }
        }
        //错误书签名格式：Error#节点名#节点实例标识
        protected string GenerateErrorBookmarkName(NativeActivityContext context)
        {
            return _errorBookmarkPrefix + this.GenerateBookmarkName(context);
        }
        //常规书签名格式：节点名#节点实例标识
        protected string GenerateBookmarkName(NativeActivityContext context)
        {
            return this.DisplayName + _split + context.ActivityInstanceId;
        }

        //派生活动要实现的实际执行主体
        protected abstract void ExecuteBody(NativeActivityContext context);

        #region IMetadataKnowable Members
        void Taobao.Activities.Statements.Flowchart.IMetadataKnowable.TellFlowNodeIndex(int index)
        {
            //未设置时
            if (this.FlowNodeIndex == _defaultIndex)
            {
                this.FlowNodeIndex = index;
                if (this.OnFlowNodeIndex != null)
                    this.OnFlowNodeIndex(this.FlowNodeIndex);
            }
            else if (this.FlowNodeIndex != index)
                throw new InvalidOperationException(string.Format(
                    "索引设置发生不一致，当前值={0}，实际运行时应={1}"
                    , this.FlowNodeIndex
                    , index));
        }
        #endregion
    }
}
