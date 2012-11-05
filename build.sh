#!/bin/bash
mono ../work-tool/nant/nant.exe $1 -D:project.config=$2 -D:runtime=mono 