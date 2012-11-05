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
using System.Web;
using System.Reflection;

using CodeSharp.Framework;
using CodeSharp.Framework.Castles;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities;

using Castle.MicroKernel.Registration;
using Taobao.Activities;
using Taobao.Workflow.Activities.Application;

namespace Taobao.Workflow.Host
{
    /// <summary>
    /// NTFE调度器服务声明
    /// 专用于独立调度器宿主
    /// </summary>
    public class SchedulerEntrance : Taobao.Infrastructure.Toolkit.AppDomains.Entrance
    {
        public override void Main()
        {
            //框架配置初始化
            SystemConfig.ConfigFilesAssemblyName = "Taobao.ConfigFiles";
            SystemConfig.Configure("TFlowEngineSchedulerNode")
                .Castle()
                .ReadCommonProperties()
                .BusinessDependency(Util.TFlowEngineReference().ToArray())
                .Resolve(this.Prepare)
                .Globalization();
                //.DefaultAppAgent("NTFE-调度节点 专用于独立调度器宿主")
                //UNDONE:启用NSF
                //.AsNsfClient();
            //设置核心使用的容器
            ActivityUtilities.Container(new Container());
            Taobao.Activities.Hosting.WorkflowInstance.IsEnableDebug = true;
            //启动调度
            DependencyResolver.Resolve<Taobao.Workflow.Activities.Hosting.IScheduler>().Run();
        }
        public override void Unload()
        {
            //停止调度
            DependencyResolver.Resolve<Taobao.Workflow.Activities.Hosting.IScheduler>().Stop();
            SystemConfig.Cleanup();
        }
        private void Prepare(WindsorResolver r)
        {
            var windsor = r.Container;
            Util.Resolve(r);
            //注册调度器
            windsor.Register(Component
                .For<Taobao.Workflow.Activities.Hosting.IScheduler>()
                .Instance(new Taobao.Workflow.Activities.Hosting.Scheduler(windsor.Resolve<ILoggerFactory>()
                    , System.Configuration.ConfigurationManager.AppSettings["SchedulerId"]
                    , SystemConfig.Settings["ntfeSchedulerInterval"]
                    , SystemConfig.Settings["ntfeSchedulerPerChargeCount"])));
        }
    }
}