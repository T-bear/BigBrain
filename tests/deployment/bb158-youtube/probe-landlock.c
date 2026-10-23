/* Synthetic primitive test, NOT a production launcher or yt-dlp policy. Linux x86_64. */
#define _GNU_SOURCE
#include <errno.h>
#include <fcntl.h>
#include <linux/audit.h>
#include <linux/filter.h>
#include <linux/landlock.h>
#include <linux/seccomp.h>
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/prctl.h>
#include <sys/resource.h>
#include <sys/socket.h>
#include <sys/stat.h>
#include <sys/syscall.h>
#include <sys/wait.h>
#include <unistd.h>

#define CHECK(x) do { if (!(x)) { fprintf(stderr,"FAIL line=%d errno=%d\n",__LINE__,errno); exit(1); } } while(0)
#define ALLOW(n) BPF_JUMP(BPF_JMP|BPF_JEQ|BPF_K, __NR_##n, 0, 1), BPF_STMT(BPF_RET|BPF_K, SECCOMP_RET_ALLOW)
static void filter(void) {
 struct sock_filter code[] = {
  BPF_STMT(BPF_LD|BPF_W|BPF_ABS, offsetof(struct seccomp_data, arch)),
  BPF_JUMP(BPF_JMP|BPF_JEQ|BPF_K, AUDIT_ARCH_X86_64, 1, 0),
  BPF_STMT(BPF_RET|BPF_K, SECCOMP_RET_KILL_PROCESS),
  BPF_STMT(BPF_LD|BPF_W|BPF_ABS, offsetof(struct seccomp_data, nr)),
  ALLOW(read), ALLOW(write), ALLOW(close), ALLOW(openat), ALLOW(newfstatat), ALLOW(fstat),
  ALLOW(lseek), ALLOW(mmap), ALLOW(mprotect), ALLOW(munmap), ALLOW(brk),
  ALLOW(rt_sigaction), ALLOW(rt_sigprocmask), ALLOW(rt_sigreturn),
  ALLOW(arch_prctl), ALLOW(set_tid_address), ALLOW(set_robust_list), ALLOW(rseq),
  ALLOW(getrandom), ALLOW(prlimit64), ALLOW(readlink), ALLOW(futex),
  ALLOW(getpid), ALLOW(getppid), ALLOW(getuid), ALLOW(getgid),
  ALLOW(execve), ALLOW(unlink), ALLOW(rename), ALLOW(truncate),
  ALLOW(exit), ALLOW(exit_group),
  BPF_STMT(BPF_RET|BPF_K, SECCOMP_RET_ERRNO|EPERM)
 };
 struct sock_fprog p={.len=sizeof(code)/sizeof(code[0]),.filter=code};
 CHECK(prctl(PR_SET_SECCOMP,SECCOMP_MODE_FILTER,&p)==0);
}
static void allow_path(int rules, const char *path, unsigned long long rights) {
 int fd=open(path,O_PATH|O_CLOEXEC); CHECK(fd>=0);
 struct landlock_path_beneath_attr rule={.allowed_access=rights,.parent_fd=fd};
 CHECK(syscall(SYS_landlock_add_rule,rules,LANDLOCK_RULE_PATH_BENEATH,&rule,0)==0);
 close(fd);
}
static void denied_open(const char *path,int flags) {
 errno=0; int fd=open(path,flags); CHECK(fd==-1 && errno==EACCES);
}
static void checks(void) {
 denied_open("/work/library",O_RDONLY);
 denied_open("/private/library",O_RDONLY);
 denied_open("/private/credentials",O_WRONLY);
 denied_open("/proc/self/environ",O_RDONLY);
 CHECK(unlink("/private/library")==-1 && errno==EACCES);
 CHECK(rename("/private/library","/work/stolen")==-1 && (errno==EACCES || errno==EXDEV));
 CHECK(truncate("/private/library",0)==-1 && errno==EACCES);
 CHECK(socket(AF_INET,SOCK_STREAM,0)==-1 && errno==EPERM);
 CHECK(socket(AF_INET6,SOCK_STREAM,0)==-1 && errno==EPERM);
 CHECK(socket(AF_INET,SOCK_DGRAM,0)==-1 && errno==EPERM);
 CHECK(socket(AF_UNIX,SOCK_STREAM,0)==-1 && errno==EPERM);
 CHECK(syscall(SYS_ptrace,0,0,0,0)==-1 && errno==EPERM);
 CHECK(syscall(SYS_unshare,0x10000000)==-1 && errno==EPERM);
 CHECK(syscall(SYS_kill,getppid(),0)==-1 && errno==EPERM);
 CHECK(syscall(SYS_clone,17,0,0,0,0)==-1 && errno==EPERM);
 CHECK(syscall(SYS_connect,-1,0,0)==-1 && errno==EPERM);
 /* Landlock does not hide metadata: explicit limitation, not confidentiality proof. */
 struct stat st; CHECK(stat("/private/library",&st)==0);
 int fd=open("/work/output",O_CREAT|O_WRONLY|O_TRUNC,0600); CHECK(fd>=0);
 CHECK(write(fd,"synthetic",9)==9); close(fd);
 puts("PASS: content denial, symlink escape, writes, direct TCP/UDP/Unix sockets, connect, ptrace, namespaces, signal and clone denial; scratch write allowed; metadata visible");
}
int main(int argc,char **argv) {
 setbuf(stdout,NULL); puts("STAGE:probe-entered");
 CHECK(getuid()==1000 && getgid()==1000);
 if(argc==2 && strcmp(argv[1],"child")==0) { checks(); puts("PASS: restrictions survive exec"); return 0; }
 CHECK(syscall(SYS_landlock_create_ruleset,0,0,LANDLOCK_CREATE_RULESET_VERSION)>=6);
 /* Same-UID world-readable synthetic secrets are accessible before restriction. */
 int fd=open("/private/library",O_RDONLY); CHECK(fd>=0); close(fd);
 fd=open("/private/credentials",O_WRONLY); CHECK(fd>=0); close(fd);
 CHECK(clearenv()==0); CHECK(chdir("/work")==0);
 CHECK(syscall(SYS_close_range,3,~0U,0)==0);
 struct rlimit limit={1048576,1048576}; CHECK(setrlimit(RLIMIT_FSIZE,&limit)==0);
 limit=(struct rlimit){2,2}; CHECK(setrlimit(RLIMIT_CPU,&limit)==0);
 limit=(struct rlimit){64*1024*1024,64*1024*1024}; CHECK(setrlimit(RLIMIT_AS,&limit)==0);
 CHECK(prctl(PR_SET_NO_NEW_PRIVS,1,0,0,0)==0);
 struct landlock_ruleset_attr attr={.handled_access_fs=(1ULL<<16)-1,
  .handled_access_net=LANDLOCK_ACCESS_NET_BIND_TCP|LANDLOCK_ACCESS_NET_CONNECT_TCP,
  .scoped=LANDLOCK_SCOPE_ABSTRACT_UNIX_SOCKET|LANDLOCK_SCOPE_SIGNAL};
 int rules=syscall(SYS_landlock_create_ruleset,&attr,sizeof(attr),0); CHECK(rules>=0);
 allow_path(rules,"/work",LANDLOCK_ACCESS_FS_READ_FILE|LANDLOCK_ACCESS_FS_READ_DIR|
  LANDLOCK_ACCESS_FS_WRITE_FILE|LANDLOCK_ACCESS_FS_MAKE_REG|LANDLOCK_ACCESS_FS_TRUNCATE);
 allow_path(rules,"/probe",LANDLOCK_ACCESS_FS_EXECUTE|LANDLOCK_ACCESS_FS_READ_FILE);
 CHECK(syscall(SYS_landlock_restrict_self,rules,0)==0); close(rules);
 puts("STAGE:landlock-enforced"); filter(); checks(); fflush(stdout);
 char *args[]={"/probe","child",NULL}; char *env[]={NULL};
 execve("/probe",args,env); CHECK(0);
}
