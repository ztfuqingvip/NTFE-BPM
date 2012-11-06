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
using System.Net;
using System.IO;
using CodeSharp.Core.Services;
using CodeSharp.Core;
using CodeSharp.Core.Utils;

namespace Taobao.Workflow.Activities.Application
{
    //FIXME:此类可在Host内实现即可

    /// <summary>
    /// 默认的用户库访问 基于NSF
    /// </summary>
    [CodeSharp.Core.Component]
    public class DefaultMethodHelper : IUserHelper, IMethodInvoker
    {
        private static Serializer _serializer = new Serializer();
        private ILog _log;
        private static IDictionary<string, Method> _methods;
        private static readonly string _getSuperior = "getSuperior";
        private string _centerUri;
        private string _userQueryService;

        static DefaultMethodHelper()
        {
            _methods = new DefaultMethods().Methods;
        }
        public DefaultMethodHelper(ILoggerFactory factory, string serviceCenterWebRootUrl, string ntfeUserQueryService)
        {
            this._log = factory.Create(typeof(DefaultMethodHelper));
            this._centerUri = serviceCenterWebRootUrl;
            this._userQueryService = ntfeUserQueryService;
        }

        //所有均返回数组字符串 只使用string参数
        #region IUserHelper Members
        public string GetSuperior(string user)
        {
            return this.Invoke(_getSuperior, user);
        }
        #endregion

        #region IMethodInvoker Members

        public IDictionary<string, Method> GetMethods()
        {
            return _methods;
        }

        public string Invoke(string method, params object[] args)
        {
            return this.InvokeMethod(_methods[method], args);
        }

        #endregion

        private string InvokeMethod(Method m, params object[] args)
        {
            try
            {
                using (var c = new WebClient())
                {
                    c.Encoding = Encoding.UTF8;
                    c.QueryString.Add("source", "NTFE-BPM");
                    c.QueryString.Add("authkey", "75DC6B572D1B940E34159DCD7FF26D8D");
                    c.QueryString.Add("_", DateTime.Now.ToString("yyyyMMddHHmmss"));

                    //HACK:针对forward特化参数，返回原始字符串
                    if (m.Service.Equals("Forward"))
                        c.QueryString.Add("scweb_format", "none");

                    if (args != null)
                        for (var i = 0; i < m.Parameters.Length; i++)
                            if (i < args.Length)
                                c.QueryString.Add(m.Parameters[i].Item1, _serializer.JsonSerialize(args[i]));

                    var url = this._centerUri + "/" + m.Service + "/" + m.Name;
                    var result = c.DownloadString(url);

                    if (this._log.IsDebugEnabled)
                        this._log.DebugFormat("执行脚本方法{0}，调用{1}，参数：{2}，返回：{3}"
                            , m.Name
                            , url
                            , string.Join("$"
                            , c.QueryString.AllKeys.Select(o => o + "=" + c.QueryString[o]))
                            , result);

                    return result;
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    throw e;
                using (var reader = new StreamReader(e.Response.GetResponseStream()))
                    throw new Exception(reader.ReadToEnd(), e);
            }
        }
    }
}