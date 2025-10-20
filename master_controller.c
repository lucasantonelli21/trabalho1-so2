#include "shared_memory.h"
#include <windows.h>

#define MAX_TRAINERS 5
#define MAX_ARENAS 3

PROCESS_INFORMATION trainers[MAX_TRAINERS];
PROCESS_INFORMATION arenas[MAX_ARENAS];

void start_process(const char* executable, const char* parameter, PROCESS_INFORMATION* pi) {
    STARTUPINFOA si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(pi, sizeof(PROCESS_INFORMATION));
    
    char command_line[256];
    snprintf(command_line, sizeof(command_line), "%s %s", executable, parameter);
    
    if (!CreateProcessA(
        NULL,           // No module name (use command line)
        command_line,   // Command line
        NULL,           // Process handle not inheritable
        NULL,           // Thread handle not inheritable
        FALSE,          // Set handle inheritance to FALSE
        0,              // No creation flags
        NULL,           // Use parent's environment block
        NULL,           // Use parent's starting directory
        &si,            // Pointer to STARTUPINFO structure
        pi)            // Pointer to PROCESS_INFORMATION structure
    ) {
        printf("Erro ao criar processo %s (error %d)\n", executable, GetLastError());
    }
}

void start_system() {
    printf("=== Iniciando Sistema de Expedição Pokémon ===\n");
    
    // Iniciar arenas
    for (int i = 0; i < MAX_ARENAS; i++) {
        char arena_id[10];
        snprintf(arena_id, sizeof(arena_id), "%d", i);
        start_process("arena.exe", arena_id, &arenas[i]);
        printf("Arena %d iniciada (PID: %d)\n", i, arenas[i].dwProcessId);
        Sleep(500); // Pequeno delay entre inícios
    }
    
    // Iniciar treinadores
    for (int i = 0; i < MAX_TRAINERS; i++) {
        char trainer_id[10];
        snprintf(trainer_id, sizeof(trainer_id), "%d", i);
        start_process("trainer.exe", trainer_id, &trainers[i]);
        printf("Treinador %d iniciado (PID: %d)\n", i, trainers[i].dwProcessId);
        Sleep(500);
    }
    
    printf("Sistema iniciado com %d arenas e %d treinadores\n", MAX_ARENAS, MAX_TRAINERS);
}

void shutdown_system() {
    printf("\n=== Finalizando Sistema ===\n");
    
    SharedMemory *shm = setup_shared_memory();
    if (shm) {
        shm->shutdown = 1;
        cleanup_shared_memory(shm);
    }
    
    // Aguardar processos finalizarem
    printf("Aguardando processos finalizarem...\n");
    Sleep(3000);
    
    // Forçar término se necessário
    for (int i = 0; i < MAX_TRAINERS; i++) {
        if (trainers[i].hProcess != NULL) {
            WaitForSingleObject(trainers[i].hProcess, 2000);
            TerminateProcess(trainers[i].hProcess, 0);
            CloseHandle(trainers[i].hProcess);
            CloseHandle(trainers[i].hThread);
        }
    }
    
    for (int i = 0; i < MAX_ARENAS; i++) {
        if (arenas[i].hProcess != NULL) {
            WaitForSingleObject(arenas[i].hProcess, 2000);
            TerminateProcess(arenas[i].hProcess, 0);
            CloseHandle(arenas[i].hProcess);
            CloseHandle(arenas[i].hThread);
        }
    }
    
    printf("Sistema finalizado.\n");
}

void monitor_system() {
    SharedMemory *shm = setup_shared_memory();
    if (!shm) return;
    
    printf("\n=== Status do Sistema ===\n");
    printf("Treinadores ativos: %d\n", shm->trainers_active);
    printf("Arenas ativas: %d\n", shm->arenas_active);
    printf("Arenas ocupadas: %d\n", shm->arenas_ocupadas);
    printf("Total de batalhas: %d\n", shm->total_batalhas);
    printf("Pokémon feridos atendidos: %d\n", shm->pokemon_feridos);
    printf("Itens na fila: %d/%d\n", shm->count, BUFFER_SIZE);
    
    cleanup_shared_memory(shm);
}

int main() {
    printf("Controlador Mestre do Sistema Pokémon\n");
    
    start_system();
    
    // Loop de monitoramento
    while (1) {
        printf("\nComandos: (m)onitor, (s)hutdown, (q)uit\n");
        printf("> ");
        
        char command;
        scanf(" %c", &command);
        
        switch (command) {
            case 'm':
            case 'M':
                monitor_system();
                break;
                
            case 's':
            case 'S':
                shutdown_system();
                return 0;
                
            case 'q':
            case 'Q':
                shutdown_system();
                return 0;
                
            default:
                printf("Comando inválido\n");
        }
    }
    
    return 0;
}