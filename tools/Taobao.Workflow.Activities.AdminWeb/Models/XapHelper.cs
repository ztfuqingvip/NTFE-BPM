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

namespace Taobao.Workflow.Activities.AdminWeb.Models
{
    /// <summary>
    ///Xap辅助类
    /// </summary>
    public class XapHelper
    {
        /// <summary>
        /// 生成Xap参数
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GenerateXapParam(string path)
        {
            var xapFile = path;
            string param;
            if (System.Diagnostics.Debugger.IsAttached)
                param = string.Format("<param name=\"source\" value=\"{0}\" />", xapFile);
            else
            {
                var xapPath = HttpContext.Current.Server.MapPath("~" + xapFile);
                var createDate = System.IO.File.GetLastWriteTime(xapPath).ToString("yyyyMMddHHmmss");
                param = string.Format("<param name=\"source\" value=\"{0}?{1}\" />", xapFile, createDate);
            }
            return param;
        }
        /// <summary>
        /// 生成SL插件下载
        /// </summary>
        /// <returns></returns>
        public static string GeneratePluginDownload()
        {
            var agent = HttpContext.Current.Request.UserAgent;
            string file;
            if (agent.IndexOf("Mac") > 0)
                file = "Silverlight_mac.dmg";
            else
                file = "Silverlight_window.exe";
            return string.Format("<a href=\"/Content/Downloads/{0}\" style=\"text-decoration:none\"><img src=\"/Content/Images/SLMedallion_CHS.png\" alt=\"Get Microsoft Silverlight\" style=\"border-style:none\" /></a>", file);
        }
    } 
}