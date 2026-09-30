#include "containment.h"
#ifdef BB132A_LIBLLAMA_PROBE
#include "llama.h"
#endif
#include <sys/socket.h>
#include <sys/wait.h>
#include <thread>
#include <iostream>
// Model-free local containment characterization. Not the production worker.
int main(int argc,char **argv) {
    if(argc!=4)return 2;
    try {
        isolation::establish(argv[1],argv[2],argv[3]);
        isolation::require(socket(AF_INET,SOCK_STREAM,0)==-1 && errno==EPERM);
        isolation::require(socket(AF_UNIX,SOCK_STREAM,0)==-1 && errno==EPERM);
        isolation::require(fork()==-1 && errno==EPERM);
        char *args[]{const_cast<char*>("true"),nullptr};
        isolation::require(execve("/usr/bin/true",args,nullptr)==-1 && errno==EPERM);
        isolation::require(open("/etc/passwd",O_RDONLY)==-1 && errno==EACCES);
        isolation::require(open(argv[2],O_WRONLY|O_TRUNC)==-1 && errno==EACCES);
        isolation::require(open("/tmp/bb132a-forbidden-write",O_WRONLY|O_CREAT,0600)==-1 && errno==EACCES);
        isolation::require(kill(getppid(),0)==-1 && errno==EPERM);
        bool ran=false;std::thread thread([&]{ran=true;});thread.join();isolation::require(ran);
        #ifdef BB132A_LIBLLAMA_PROBE
        llama_log_set([](ggml_log_level,const char*,void*){},nullptr);
        isolation::require(ggml_backend_load((std::string(argv[1])+"/libggml-cpu-haswell.so").c_str())!=nullptr);
        llama_backend_init();llama_backend_free();
#endif
        int fd=open(argv[2],O_RDONLY);isolation::require(fd>=0);close(fd);
        std::cout<<"PASS network, process creation, exec, foreign read, write, signals denied; thread/read allowed\n";
        return 0;
    }catch(...) {std::cout<<"FAIL containment probe errno="<<errno<<"\n";return 3;}
}
