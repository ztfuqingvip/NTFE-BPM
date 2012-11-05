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
using CodeSharp.Core.DomainBase;
using Taobao.Activities.Hosting;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程信息
    /// </summary>
    public class Process : EntityBase<Guid>, IAggregateRoot
    {
        /// <summary>
        /// 获取流程标题
        /// </summary>
        public virtual string Title { get; private set; }
        /// <summary>
        /// 获取流程发起时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取流程结束时间
        /// </summary>
        public virtual DateTime? FinishTime { get; private set; }
        /// <summary>
        /// 获取流程对应的流程类型/版本信息
        /// </summary>
        public virtual ProcessType ProcessType { get; private set; }
        /// <summary>
        /// 获取发起人
        /// </summary>
        public virtual User Originator { get; private set; }
        /// <summary>
        /// 获取流程状态
        /// </summary>
        public virtual ProcessStatus Status { get; private set; }
        /// <summary>
        /// 获取或设置流程优先级
        /// </summary>
        public virtual int Priority { get; set; }
      
        //获取流程数据字段列表
        private IList<ProcessDataField> _dataFields { get; set; }
        /// <summary>
        /// 作为子流程其所属的父流程的ID
        /// </summary>
        public virtual Guid? ParentProcessId { get; private set; }

        //为优化，不通过process维护InternalWorkflowInstance
        ///// <summary>
        ///// 获取工作流实例
        ///// </summary>
        //protected internal virtual InternalWorkflowInstance Instance { get; set; }

        //配合调度器串行化调度改造而增加此项
        private string _chargingBy { get; set; }

        protected Process()
        {
            this.Status = ProcessStatus.New;
            this.CreateTime = DateTime.Now;
            this._dataFields = new List<ProcessDataField>();
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="originator"></param>
        public Process(string title, ProcessType type, User originator)
            : this(title, type, originator, 0, null) { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="originator"></param>
        /// <param name="priority"></param>
        /// <param name="inputs"></param>
        public Process(string title, ProcessType type, User originator, int priority, IDictionary<string, string> inputs)
            : this()
        {
            this.Title = title;
            this.ProcessType = type;
            this.Originator = originator;
            this.Priority = priority;

            this.UpdateDataFields(inputs);
            this.UpdateCurrentNode(0);
            
            this.Validate();
        }
        /// <summary>
        /// 初始化子流程
        /// </summary>
        /// <param name="title"></param>
        /// <param name="type"></param>
        /// <param name="originator"></param>
        /// <param name="priority"></param>
        /// <param name="inputs"></param>
        /// <param name="parent"></param>
        public Process(string title, ProcessType type, User originator, int priority, IDictionary<string, string> inputs, Process parent)
            : this(title, type, originator, priority, inputs)
        {
            if (parent == null)
                throw new InvalidOperationException("parent不能为空");
            if (parent.ID == this.ID)
                throw new InvalidOperationException("不能将流程设置为自己的父流程");

            this.ParentProcessId = parent.ID;
        }

        /// <summary>
        /// 更新数据字段
        /// <remarks>不支持更新内置变量</remarks>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public virtual void UpdateDataField(string name, string value)
        {
            this.UpdateDataField(name, value, false);
        }
        /// <summary>
        /// 更新数据字段集合
        /// <remarks>若包含内置变量则忽略该项</remarks>
        /// </summary>
        /// <param name="overrides"></param>
        public virtual void UpdateDataFields(IDictionary<string, string> overrides)
        {
            if (overrides == null) return;
            overrides.ToList().ForEach(o => this.UpdateDataField(o.Key, o.Value));
        }
        /// <summary>
        /// 获取数据字段集合
        /// <remarks>不会返回内置变量</remarks>
        /// </summary>
        /// <returns></returns>
        public virtual IDictionary<string, string> GetDataFields()
        {
            return this._dataFields
                .Select(o => new KeyValuePair<string, string>(o.Name, o.Value))
                //过滤内置变量
                .Where(o => o.Key != WorkflowBuilder.Variable_CurrentNode)
                .ToDictionary(o => o.Key, o => o.Value);
        }

        //获取数据字段集合，完整返回包含内置变量
        protected internal virtual IDictionary<string, object> GetInputs()
        { 
            return this._dataFields
                .Select(o => new KeyValuePair<string, object>(o.Name, o.Value))
                .ToDictionary(o => o.Key, o => o.Value);
        }
        //获取当前节点索引
        protected internal virtual int GetCurrentNode()
        {
            var dic = this.GetInputs();
            object current;
            return dic.TryGetValue(WorkflowBuilder.Variable_CurrentNode, out current) ? Convert.ToInt32(current) : 0;
        }
        //更新当前节点索引
        protected internal virtual void UpdateCurrentNode(int i)
        {
            AssertHelper.ThrowIfInvalidFlowNodeIndex(i);
            this.UpdateDataField(WorkflowBuilder.Variable_CurrentNode, i.ToString(), true);
        }
        //设置主键
        protected internal virtual void SetId(Guid id)
        {
            if (id == Guid.Empty)
                throw new InvalidOperationException("不合法的id");
            this.ID = id;
        }
        protected internal virtual void ChangeStatus(ProcessStatus status)
        {
            this.ChangeStatus(status, false,false);
        }
        //标记为error状态，通常只有系统级别异常才会导致此情况，不可轻易设置
        protected internal virtual void MarkAsError(Exception reason)
        {
            this.ChangeStatus(ProcessStatus.Error, false, true);
        }
        //标记为active状态
        protected internal virtual void MarkAsActive()
        {
            this.ChangeStatus(ProcessStatus.Active, true,false);
        }
        //修改对应的类型信息并尝试将当前节点索引修正到新类型的对应索引上
        protected internal virtual void ChangeProcessType(ProcessType target)
        {
            if (this.Status == ProcessStatus.Deleted
               || this.Status == ProcessStatus.Completed)
                throw new InvalidOperationException("该流程实例的流程类型不可变更");
            if (!target.Name.Equals(this.ProcessType.Name))
                throw new InvalidOperationException("只允许变更到同一流程类型的其他版本");
            if (target.Version.Equals(this.ProcessType.Version))
                throw new InvalidOperationException("已经是目标版本，无需变更");
        
            var current = this.ProcessType.GetActivitySetting(this.GetCurrentNode());

            //当前流程定义损坏的情况
            if (current == null)
                this.UpdateCurrentNode(0);
            else
            {
                var setting = target.GetActivitySetting(current.ActivityName);

                if (setting == null)
                    //HACK:目前对于不对等的流程版本禁止切换
                    throw new InvalidOperationException("没有在目标流程定义中找到节点的“" + current.ActivityName + "”定义，无法切换");

                this.UpdateCurrentNode(target == null ? 0 : setting.FlowNodeIndex);

            }
            this.ProcessType = target;
        }

        protected internal virtual void SetChargingBy(string charginBy)
        {
            this._chargingBy = charginBy;
        }
        protected internal virtual string GetChargingBy()
        {
            return this._chargingBy;
        }

        //更新流程变量
        private void UpdateDataField(string name, string value, bool enable)
        {
            //忽略内置变量
            if (!enable && name == WorkflowBuilder.Variable_CurrentNode)
                return;

            var field = this._dataFields.FirstOrDefault(o => o.Name.Equals(name));
            if (field == null)
                this._dataFields.Add(field = new ProcessDataField(name));
            field.Value = value;
        }
        private void ChangeStatus(ProcessStatus status, bool enableActive, bool enableError)
        {
            if (this.Status == ProcessStatus.Deleted
                || this.Status == ProcessStatus.Completed)
                throw new InvalidOperationException("该流程实例状态不可变更");
            if (status == ProcessStatus.New)
                throw new InvalidOperationException("不允许将流程实例置为New状态");
            if (!enableActive && status == ProcessStatus.Active)
                throw new InvalidOperationException("不支持直接将流程实例置为Active状态");
            if (!enableError && status == ProcessStatus.Error)
                throw new InvalidOperationException("不支持直接将流程实例置为Error状态");

            this.Status = status;

            if (this.Status == ProcessStatus.Completed)
                this.FinishTime = DateTime.Now;
        }
        private void Validate()
        {
            if (string.IsNullOrEmpty(this.Title))
                throw new InvalidOperationException("Title不能为空");
            if (this.ProcessType == null)
                throw new InvalidOperationException("ProcessType不能为空");
            if (this.Originator == null)
                throw new InvalidOperationException("Originator不能为空");
        }

        /// <summary>
        /// 工作流实例树信息
        /// </summary>
        public class InternalWorkflowInstance
        {
            /// <summary>
            /// 获取序列化后的工作流实例文本
            /// </summary>
            public virtual string Serialized { get; private set; }
            /// <summary>
            /// 获取工作流实例
            /// </summary>
            public virtual WorkflowInstance WorkflowInstance
            {
                get
                {
                    return WorkflowBuilder.Deserialize(typeof(WorkflowInstance), this.Serialized) as WorkflowInstance;
                }
                set
                {
                    if (value == null)
                        throw new InvalidOperationException("WorkflowInstance不能为空");

                    this.Serialized = WorkflowBuilder.Serialize(value, typeof(WorkflowInstance));
                }
            }

            protected InternalWorkflowInstance() { }
            public InternalWorkflowInstance(string serialized)
            {
                this.Serialized = serialized;
            }
            public InternalWorkflowInstance(WorkflowInstance instance)
            {
                this.WorkflowInstance = instance;
            }
        }
    }
}