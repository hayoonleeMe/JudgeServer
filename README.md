# JudgeServer


## Dockerfile

FROM gcc:latest

RUN apt-get update && apt-get install -y time

WORKDIR /app

COPY . .

ENV DIR_NAME=your_dir_name

CMD gcc ${DIR_NAME}/code.c -o ${DIR_NAME}/output 2> ${DIR_NAME}/compileError.txt -O2 -Wall -lm -static -std=gnu11 && /usr/bin/time -f "%e\n%M" -o ${DIR_NAME}/stat.txt -- ${DIR_NAME}/output < ${DIR_NAME}/input.txt > ${DIR_NAME}/result.txt 2>> ${DIR_NAME}/runtimeError.txt
