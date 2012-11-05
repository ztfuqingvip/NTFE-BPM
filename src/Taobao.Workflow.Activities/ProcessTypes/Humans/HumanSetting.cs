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
using Taobao.Activities.Statements;
using Taobao.Activities;
using CodeSharp.Core.DomainBase;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 人工节点设置信息
    /// </summary>
    public class HumanSetting : CustomSetting
    {
        /// <summary>
        /// 获取Slot分发模式
        /// </summary>
        public virtual SlotDistributionMode SlotMode { get; private set; }
        /// <summary>
        /// 获取slot数量
        /// <remarks>若为-1则忽略</remarks>
        /// </summary>
        public virtual int SlotCount { get; private set; }
        /// <summary>
        /// 获取人工节点的任务地址
        /// </summary>
        public virtual string Url { get; private set; }

        private string _actions
        {
            get { return string.Join("$", this.Actions); }
            set { this.Actions = (value ?? "").Split('$'); }
        }
        /// <summary>
        /// 获取动作
        /// </summary>
        public virtual string[] Actions { get; private set; }
        /// <summary>
        /// 获取执行人规则
        /// </summary>
        public virtual HumanActionerRule ActionerRule { get; private set; }
        /// <summary>
        /// 获取升级规则，可能为空
        /// </summary>
        public virtual HumanEscalationRule EscalationRule { get; private set; }

        /// <summary>
        /// 获取是否使用Slot
        /// </summary>
        public virtual bool IsUsingSlot { get { return this.SlotCount > 0; } }

        protected HumanSetting() : base() { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">所在流程图节点的索引</param>
        /// <param name="activityName">节点名称</param>
        /// <param name="actions">可执行的操作</param>
        /// <param name="slotCount">默认为-1，将忽略该设置</param>
        /// <param name="slotMode">slot分发模式</param>
        /// <param name="startRule">不设置则留空</param>
        /// <param name="actionerRule"></param>
        /// <param name="finishRule">不设置则留空</param>
        /// <param name="escalationRule">不设置则留空</param>
        /// <param name="isChildOfActivity">是否是子节点</param>
        public HumanSetting(int flowNodeIndex
            , string activityName
            , string[] actions
            , int slotCount
            , SlotDistributionMode slotMode
            , string url
            , StartRule startRule
            , HumanActionerRule actionerRule
            , FinishRule finishRule
            , HumanEscalationRule escalationRule
            , bool isChildOfActivity)
            : base(flowNodeIndex
            , activityName
            , startRule
            , finishRule
            , isChildOfActivity)
        {
            this.Actions = actions;
            this.SlotCount = slotCount;
            this.SlotMode = slotMode;
            this.Url = url;
            this.ActionerRule = actionerRule;
            this.EscalationRule = escalationRule;

            this.Validate();
        }

        public override ActivitySetting Clone()
        {
            return new HumanSetting(this.FlowNodeIndex
                , this.ActivityName
                , this.Actions
                , this.SlotCount
                , this.SlotMode
                , this.Url
                , this.StartRule
                , this.ActionerRule
                , this.FinishRule
                , this.EscalationRule
                , this.IsChildOfActivity);
        }

        private void Validate()
        {
            if (this.Actions == null || this.Actions.Length == 0 || this.Actions.Count(o => string.IsNullOrEmpty(o)) > 0)
                throw new InvalidOperationException("必须提供至少一个action，且每个action不能为空");
            if (this.ActionerRule == null)
                throw new InvalidOperationException("ActionerRule不能为空");
        }

        /// <summary>
        /// Slot的分发模式
        /// </summary>
        public enum SlotDistributionMode
        {
            /// <summary>
            /// 一次性分发 
            /// </summary>
            AllAtOnce = 0,
            /// <summary>
            /// 按顺序分发
            /// </summary>
            OneAtOnce = 1
        }
    }
}