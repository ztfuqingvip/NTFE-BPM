NTFE-BPM
====

NTFE is a taobao flow engine written by .net/mono/c#. 

NTFE-BPM is Business Process Manangement found on NTFE-Core, 

also, it can run on wf4 or other micro-kernel engine.

NTFE-Core is complex, but BPM simple and effective, using written in a simple three-tier, like an common app and easy to learn.

## Showcase


## Build

Depends on https://github.com/ali-ent/work-tool.git for building tools.
Depends on https://github.com/ali-ent/NTFE-Core.git for core engine.

.NET
```shell
nuget.install.bat
build host [Debug|Test|Release]
```

MONO (build via xbuild)
```shell
sh nuget.install.sh
```

## Run

.NET
```shell
run.ps1
```

MONO
```shell
sh run.sh
```

### About Taobao WorkflowFoundation Projects

	NTFE=Taobao FlowEngine written with .Net(c#)

	Found at 2011.08

	author:
	houkun@taobao.com
	xiaoxuan.lp@taobao.com

	Core:
	Taobao.Activities

	Designer:
	Taobao.Workflow.Activities.Designer(rehost wf4 mode)

	Outside:
	Taobao.Workflow.Activities(actually run as a workflowengine)
	Taobao.Workflow.Activities.Client(provide client api)
	Taobao.Workflow.Host

## lib upgrade

castle/nhibernate upgrade record:
https://github.com/codesharp/infrastructure/blob/master/upgrade.md

[More](upgrade.md)


## NTFE-BPM License

![GPL](http://www.gnu.org/graphics/gplv3-127x51.png)

	[GPL](http://www.gnu.org/copyleft/gpl.html)
	

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

## License

- NTFE-Core https://github.com/ali-ent/NTFE-Core

- Code# Infrastructure https://github.com/codesharp/infrastructure

	Copyright (c) CodeSharp.  All rights reserved. - http://www.icodesharp.com/

	Licensed under the Apache License, Version 2.0 (the "License");

	you may not use this file except in compliance with the License.

	You may obtain a copy of the License at

		 http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, 

	software distributed under the License is distributed on an "AS IS" BASIS, 

	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

	See the License for the specific language governing permissions and limitations under the License.

- Log4net

- Castle

- NHibernate

- FluentNHibernate

- Apploader https://github.com/ali-ent/apploader

- AppAgent https://github.com/ali-ent/AppAgent
