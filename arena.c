#include "shared_memory.h"

volatile int running = 1;

BOOL WINAPI console_handler(DWORD signal) {
    if (signal == CTRL_C_EVENT) {
        running = 0;
        return TRUE;
    }
    return FALSE;
}

void simular_batalha(PokemonRequest pokemon, int arena_id) {
    printf("Arena %d: Iniciando batalha com %s (Nível %d, Prioridade: %d)\n", 
           arena_id, pokemon.nome, pokemon.nivel, pokemon.prioridade);
    Sleep(rand() % 3000 + 2000); // 2-5 segundos para batalha
    printf("Arena %d: Batalha com %s concluída\n", arena_id, pokemon.nome);
}

int main(int argc, char *argv[]) {
    if (argc != 2) {
        printf("Uso: arena <arena_id>\n");
        return 1;
    }
    
    SetConsoleCtrlHandler(console_handler, TRUE);
    
    int arena_id = atoi(argv[1]);
    printf("Arena %d iniciada (PID: %d)\n", arena_id, GetCurrentProcessId());
    
    SharedMemory *shm = setup_shared_memory();
    if (!shm) {
        return 1;
    }
    
    // Registrar arena ativa
    WaitForSingleObject(hMutex, INFINITE);
    shm->arenas_active++;
    ReleaseMutex(hMutex);
    
    srand((unsigned int)time(NULL) + arena_id);
    
    while (running) {
        if (shm->shutdown) {
            break;
        }
        
        PokemonRequest request;
        
        if (consumir_pokemon(shm, &request)) {
            // Atualizar estatística de arenas ocupadas
            WaitForSingleObject(hMutex, INFINITE);
            shm->arenas_ocupadas++;
            ReleaseMutex(hMutex);
            
            simular_batalha(request, arena_id);
            
            // Atualizar estatísticas gerais
            WaitForSingleObject(hMutex, INFINITE);
            shm->total_batalhas++;
            if (request.prioridade > 0) {
                shm->pokemon_feridos++;
            }
            shm->arenas_ocupadas--;
            ReleaseMutex(hMutex);
        } else {
            Sleep(1000); // Buffer vazio, aguarda 1 segundo
        }
    }
    
    // Desregistrar arena
    WaitForSingleObject(hMutex, INFINITE);
    shm->arenas_active--;
    ReleaseMutex(hMutex);
    
    printf("Arena %d finalizada\n", arena_id);
    cleanup_shared_memory(shm);
    return 0;
}