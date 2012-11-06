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

namespace Host
{
    /// <summary>
    /// NTFE服务声明
    /// <remarks>仅用于提供NTFE-BPM的客户端服务</remarks>
    /// </summary>
    public class ServiceEntrance : Taobao.Infrastructure.Toolkit.AppDomains.Entrance
    {
        public override void Main()
        {
            //框架配置初始化
            SystemConfig.ConfigFilesAssemblyName = "Host";
            SystemConfig.Configure("TFlowEngineServiceNode")
                .Castle()
                //不再使用内部配置服务
                //.ReadCommonProperties()
                .BusinessDependency(Util.TFlowEngineReference().ToArray())
                .Resolve(this.Prepare)
                .Globalization();
                //.DefaultAppAgent("NTFE-服务节点，提供公开的api调用")
                //.AsNsfClient(NetHelper.GetIPv4Address()
                //, int.Parse(SystemConfig.Settings["ntfeTcpPort"])
                //, AllServiceTypesToRegist().ToArray());
            //基本可用性检查
            this.DoTest();
        }
        public override void Unload()
        {
            SystemConfig.Cleanup();
        }
        private void Prepare(WindsorResolver r)
        {
            var windsor = r.Container;
            Util.Resolve(r);
        }
        //要注册的服务
        private IEnumerable<Type> AllServiceTypesToRegist()
        {
            yield return typeof(Taobao.Workflow.Activities.Client.ITFlowEngine);
            yield return typeof(Taobao.Workflow.Activities.Management.ITFlowEngine);
            //设计器/流程转换器服务
            yield return typeof(Taobao.Workflow.Activities.Converters.IConverterService);
        }
        private void DoTest()
        {
            this.AllServiceTypesToRegist().ToList().ForEach(o => DependencyResolver.Resolve(o));
            AppDomain.CurrentDomain.UnhandledException += (s, e) => { };
        }
    }
}