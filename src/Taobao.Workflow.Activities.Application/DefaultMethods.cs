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

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 方法定义
    /// </summary>
    public class DefaultMethods
    {
        //key=最终显示名 method.Name=实际映射的远程方法
        private static IDictionary<string, Method> _methods = new Dictionary<string, Method>();

        static DefaultMethods()
        {
            //TODO:在此静态定义脚本方法，后期考虑动态化
            #region 内置用户查询服务TFlowEngineUserService
            var userService = "Taobao.Framework.Services.TFlowEngineUserService";
            _methods.Add("getSuperior"
                , new Method("GetSuperior"
                    , userService
                    , new Tuple<string, Type, string>("user", typeof(string), "可以是域账号/花名/用户ID")) { Description = "getSuperior-获取主管" });
            _methods.Add("getSuperiors"
                , new Method("GetSuperiors"
                    , userService
                    , new Tuple<string, Type, string>("user", typeof(string), "可以是域账号/花名/用户ID")
                    , new Tuple<string, Type, string>("upper", typeof(int), "向上查找的层数")
                    , new Tuple<string, Type, string>("lessThenLevel", typeof(int), "查找到小于该级别")) { Description = "getSuperiors-获取N级主管列表" });
            _methods.Add("getUpperSuperior"
                , new Method("GetUpperSuperior"
                    , userService
                    , new Tuple<string, Type, string>("user", typeof(string), "可以是域账号/花名/用户ID")
                    , new Tuple<string, Type, string>("upper", typeof(int), "向上查找的层数")
                    , new Tuple<string, Type, string>("lessThenLevel", typeof(int), "查找到小于该级别")) { Description = "getUpperSuperior-获取N级主管" });
            _methods.Add("getSuperiorsInRole"
                , new Method("GetSuperiorsInRole"
                    , userService
                    , new Tuple<string, Type, string>("user", typeof(string), "可以是域账号/花名/用户ID")
                    , new Tuple<string, Type, string>("upper", typeof(int), "向上查找的层数")
                    , new Tuple<string, Type, string>("role", typeof(string), "角色名称或key")) { Description = "getSuperiorsInRole-获取处于指定角色的N级主管" });
            _methods.Add("getUsers"
                , new Method("GetUsers"
                    , userService
                    , new Tuple<string, Type, string>("role", typeof(string), "角色名称或key")
                    , new Tuple<string, Type, string>("user", typeof(string), "可以是域账号/花名/用户ID")
                    , new Tuple<string, Type, string>("categoryId", typeof(string), "业务分类标识")) { Description = "getUsers-获取关联业务分类/关联角色/关联用户的用户列表，兼容老接口" });
            #endregion

            #region 内置函数 此处仅是定义其使用说明，具体逻辑需要在ScriptParser中定义
            //update
            _methods.Add("updateDataField"
            , new Method("updateDataField"
                , string.Empty
                , new Tuple<string, Type, string>("key", typeof(string), "流程变量名，使用变量赋值时注意实际使用的是该变量的值")
                , new Tuple<string, Type, string>("value", typeof(string), "要设置的值")) { Description = "updateDataField-更新流程变量", Void = true });
            //split
            _methods.Add("split"
            , new Method("split"
                , string.Empty
                , new Tuple<string, Type, string>("separator", typeof(string), "分隔符")
                , new Tuple<string, Type, string>("input", typeof(string), "输入的字符串"))
                {
                    Description = "split-提供对字符串的拆分功能，返回字符串数组",
                    Void = false
                });
            #endregion

            //转发服务
            _methods.Add("forward"
              , new Method("Http"
                  , "Forward"
                  , new Tuple<string, Type, string>("target", typeof(string), "要调用的目前服务配置键，即配置表ServceiList中定义的键")
                  , new Tuple<string, Type, string>("authParameters", typeof(string), "验证参数，若需要额外的身份验证则设置此项，json字符串")
                  , new Tuple<string, Type, string>("parameters", typeof(string), "调用时附加的参数，json字符串")) { Description = "forward-转发服务，提供对服务Taobao.Facades.IForwardService的调用，返回目标地址返回的原始文本" });

            //按需添加服务中心服务
        }

        /// <summary>
        /// 获取方法
        /// </summary>
        public IDictionary<string, Method> Methods { get { return _methods; } }
    }
}
