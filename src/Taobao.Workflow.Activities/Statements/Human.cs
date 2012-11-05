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
using Taobao.Activities;
using System.Linq.Expressions;
using Taobao.Activities.Expressions;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 人工活动
    /// </summary>
    public class Human : Custom
    {
        /// <summary>
        /// 获取执行人
        /// </summary>
        public InArgument<string[]> Actioners { get; private set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="actioners"></param>
        public Human(Expression<Func<ActivityContext, string[]>> actioners)
            : this(new LambdaValue<string[]>(actioners)) { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        /// <param name="actioners"></param>
        public Human(int flowNodeIndex, Expression<Func<ActivityContext, string[]>> actioners)
            : this(flowNodeIndex, new LambdaValue<string[]>(actioners)) { }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="actioners"></param>
        public Human(Activity<string[]> actioners)
            : base()
        {
            this.Actioners = new InArgument<string[]>(actioners);
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="flowNodeIndex">对应的索引</param>
        /// <param name="actioners"></param>
        public Human(int flowNodeIndex, Activity<string[]> actioners)
            : base(flowNodeIndex)
        {
            this.Actioners = new InArgument<string[]>(actioners);
        }


        //调整actioner获取逻辑以适应错误重试粒度要求 20120506
        public IActionersHelper Helper { get; private set; }
        public Human(IActionersHelper helper)
            : base()
        {
            this.Helper = helper;
            //由于兼容需要必须保留此初始化 
            //HACK:依赖序列化的糟糕兼容设计导致
            this.Actioners = new InArgument<string[]>(new GetUsers("originator"));
        }
        public Human(int flowNodeIndex, IActionersHelper helper)
            : base(flowNodeIndex)
        {
            this.Helper = helper;
            //由于兼容需要必须保留此初始化
            this.Actioners = new InArgument<string[]>(new GetUsers("originator"));
        }

        protected override void ExecuteBody(NativeActivityContext context)
        {
            //var actioners = this.Actioners.Get(context);
            var actioners = this.Helper.GetActioners(context);

            if (actioners == null || actioners.Length == 0)
                throw new InvalidOperationException("没有执行人");
            //创建人工节点实例信息
            context.GetExtension<HumanExtension>().AddHumanTask(context
                //HACK:创建任务书签，仅用于活动暂停
                //任务活动不涉及具体的工作流任务模式的实现（如会签等）,由任务执行逻辑实现
                , context.CreateBookmark(this.GenerateBookmarkName(context)
                , this.OnBookmarkCallback)
                , this.DisplayName
                , this.FlowNodeIndex
                , actioners);
        }
        private void OnBookmarkCallback(NativeActivityContext context, Bookmark bookmark, object value)
        {
            if (this.Result != null)
                this.Result.Set(context, value == null ? string.Empty : value.ToString());
            context.RemoveBookmark(bookmark);
        }
    }
    //HACK:由于core调度的参数调度支持暂无法支持有友好重试，故增加此设计将actioner获取与human主体逻辑合并处理，待core优化后再还原参数调度形式
    public interface IActionersHelper
    {
        string[] GetActioners(NativeActivityContext context);
    }
}