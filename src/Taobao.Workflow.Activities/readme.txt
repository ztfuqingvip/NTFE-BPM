NTFE-BPM工作流应用，面向应用
@author:houkun@taobao.com|xiaoxuan.lp@taobao.com
@date:201108

当前实现是面向NTFE.Flowchart而设计

无Context（身份等）设计，请在最终应用层进行控制

概念：
Workflow=工作流，指代NTFE-Core核心引擎的工作流概念，不等同Process
PorcessType=流程定义/类型，包含一个Workflow定义
Process=业务流程实例，BPM应用的流程实例
Activity=活动，核心引擎中描述工作流的活动，在本项目中称为“流程节点”
ActivityInstance=活动实例，在本项目中称为“流程节点实例”
WorkItem=人工活动工作项，称为“任务”

ActivitySetting=节点设置，相当于activity基本定义

Human=人工
SubProcess=子流程

