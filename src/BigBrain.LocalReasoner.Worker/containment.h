#pragma once
// Linux-only bootstrap, before reading untrusted context or loading weights. No process supervisor.
#include <linux/audit.h>
#include <linux/filter.h>
#include <linux/landlock.h>
#include <linux/seccomp.h>
#include <linux/magic.h>
#include <sys/prctl.h>
#include <sys/resource.h>
#include <sys/stat.h>
#include <sys/statfs.h>
#include <sys/syscall.h>
#include <sched.h>
#include <fcntl.h>
#include <dirent.h>
#include <unistd.h>
#include <cerrno>
#include <cstddef>
#include <cstring>
#include <string>
#include <vector>
#include <stdexcept>

namespace isolation {
inline void require(bool condition) { if (!condition) throw std::runtime_error("containment unavailable"); }
inline std::string read_file(int dir, const char *name) {
    int fd=openat(dir,name,O_RDONLY|O_CLOEXEC|O_NOFOLLOW); require(fd>=0);
    char b[128]; auto n=read(fd,b,sizeof(b)); close(fd); require(n>=0 && n<128);
    return std::string(b,n);
}
inline void enter_cgroup(const char *path) {
    int dir=open(path,O_RDONLY|O_DIRECTORY|O_CLOEXEC|O_NOFOLLOW); require(dir>=0);
    struct stat st{}; struct statfs fs{};
    require(fstat(dir,&st)==0 && fstatfs(dir,&fs)==0 && fs.f_type==CGROUP2_SUPER_MAGIC &&
            st.st_uid==geteuid() && (st.st_mode&0077)==0);
    // Dedicated empty group provisioned by the trusted local acceptance operator, never model input.
    require(read_file(dir,"cgroup.procs").empty());
    require(read_file(dir,"memory.max")=="4294967296\n" && read_file(dir,"memory.swap.max")=="0\n" &&
            read_file(dir,"cpu.max")=="200000 100000\n" && read_file(dir,"pids.max")=="32\n" &&
            read_file(dir,"memory.oom.group")=="1\n");
    int fd=openat(dir,"cgroup.procs",O_WRONLY|O_CLOEXEC|O_NOFOLLOW); require(fd>=0);
    auto own=std::to_string(getpid()); require(write(fd,own.data(),own.size())==(ssize_t)own.size());
    close(fd); close(dir);
}
inline void limit(int resource, rlim_t value) { struct rlimit l{value,value}; require(setrlimit(resource,&l)==0); }
inline void landlock(const char *runtime, const char *model) {
    int abi=syscall(SYS_landlock_create_ruleset,nullptr,0,LANDLOCK_CREATE_RULESET_VERSION);
    require(abi>=3);
    struct landlock_ruleset_attr attr{};
    attr.handled_access_fs=(1ULL<<15)-1; // ABI3: every FS right through REFER and TRUNCATE.
    int rules=syscall(SYS_landlock_create_ruleset,&attr,sizeof(attr),0); require(rules>=0);
    for (const auto &entry : {std::pair<const char*,bool>{runtime,true},{model,false}}) {
        int fd=open(entry.first,O_PATH|O_CLOEXEC|O_NOFOLLOW); require(fd>=0);
        struct stat st{}; require(fstat(fd,&st)==0 && (entry.second?S_ISDIR(st.st_mode):S_ISREG(st.st_mode)));
        struct landlock_path_beneath_attr rule{};
        rule.parent_fd=fd; rule.allowed_access=LANDLOCK_ACCESS_FS_READ_FILE | (entry.second?LANDLOCK_ACCESS_FS_READ_DIR:0);
        require(syscall(SYS_landlock_add_rule,rules,LANDLOCK_RULE_PATH_BENEATH,&rule,0)==0); close(fd);
    }
    require(prctl(PR_SET_NO_NEW_PRIVS,1,0,0,0)==0);
    require(syscall(SYS_landlock_restrict_self,rules,0)==0); close(rules);
}
inline void seccomp() {
    std::vector<sock_filter> f;
    auto stmt=[&](unsigned short c,unsigned k){f.push_back(BPF_STMT(c,k));};
    auto jump=[&](unsigned short c,unsigned k,unsigned char t,unsigned char e){f.push_back(BPF_JUMP(c,k,t,e));};
    stmt(BPF_LD|BPF_W|BPF_ABS,offsetof(seccomp_data,arch));
    jump(BPF_JMP|BPF_JEQ|BPF_K,AUDIT_ARCH_X86_64,1,0); stmt(BPF_RET|BPF_K,SECCOMP_RET_KILL_PROCESS);
    stmt(BPF_LD|BPF_W|BPF_ABS,offsetof(seccomp_data,nr));
    jump(BPF_JMP|BPF_JEQ|BPF_K,SYS_clone3,0,1);stmt(BPF_RET|BPF_K,SECCOMP_RET_ERRNO|ENOSYS);
    jump(BPF_JMP|BPF_JEQ|BPF_K,SYS_clone,0,5);
    stmt(BPF_LD|BPF_W|BPF_ABS,offsetof(seccomp_data,args[0]));
    stmt(BPF_ALU|BPF_AND|BPF_K,CLONE_THREAD|CLONE_VM|CLONE_SIGHAND);
    jump(BPF_JMP|BPF_JEQ|BPF_K,CLONE_THREAD|CLONE_VM|CLONE_SIGHAND,0,1);
    stmt(BPF_RET|BPF_K,SECCOMP_RET_ALLOW);stmt(BPF_RET|BPF_K,SECCOMP_RET_ERRNO|EPERM);
    // No exec/fork/vfork/socket/connect/listen/ptrace/kill or namespace/cgroup administration.
    for (int call : {SYS_read,SYS_write,SYS_readv,SYS_writev,SYS_close,SYS_openat,SYS_newfstatat,SYS_fstat,SYS_statx,
         SYS_lseek,SYS_pread64,SYS_mmap,SYS_mprotect,SYS_munmap,SYS_brk,SYS_madvise,SYS_futex,SYS_sched_yield,
         SYS_rt_sigaction,SYS_rt_sigprocmask,SYS_rt_sigreturn,SYS_sigaltstack,SYS_set_robust_list,SYS_rseq,
         SYS_set_tid_address,SYS_gettid,SYS_getpid,SYS_getuid,SYS_geteuid,SYS_getgid,SYS_getegid,
         SYS_clock_gettime,SYS_clock_nanosleep,SYS_nanosleep,SYS_gettimeofday,SYS_getrusage,SYS_times,
         SYS_sched_getaffinity,SYS_getrandom,SYS_fcntl,SYS_readlink,SYS_readlinkat,SYS_access,SYS_faccessat,
         SYS_getdents64,SYS_uname,SYS_sysinfo,SYS_exit,SYS_exit_group}) {
        jump(BPF_JMP|BPF_JEQ|BPF_K,call,0,1);stmt(BPF_RET|BPF_K,SECCOMP_RET_ALLOW);
    }
    stmt(BPF_RET|BPF_K,SECCOMP_RET_ERRNO|EPERM);
    struct sock_fprog program{(unsigned short)f.size(),f.data()};
    require(syscall(SYS_seccomp,SECCOMP_SET_MODE_FILTER,SECCOMP_FILTER_FLAG_TSYNC,&program)==0);
}
inline void establish(const char *runtime,const char *model,const char *cgroup,rlim_t cpu_seconds=60) {
    require(getuid()==geteuid() && geteuid()!=0);
    DIR *tasks=opendir("/proc/self/task");require(tasks!=nullptr);
    int threads=0;while(auto *entry=readdir(tasks)) if(entry->d_name[0]!='.') ++threads;
    closedir(tasks);require(threads==1); // Landlock must precede every computation thread.
    enter_cgroup(cgroup);
    // AS additionally bounds mappings whose page-cache charge may belong to the acquisition process.
    require(cpu_seconds==60 || cpu_seconds==360);
    limit(RLIMIT_AS,4294967296ULL);limit(RLIMIT_CORE,0);limit(RLIMIT_FSIZE,0);limit(RLIMIT_NOFILE,64);limit(RLIMIT_CPU,cpu_seconds);
    // Prevent other same-UID processes from reading memory/attaching via normal ptrace policy.
    require(prctl(PR_SET_DUMPABLE,0,0,0,0)==0);
    landlock(runtime,model);seccomp();
}
}
