#include "shared_memory.h"
#include <signal.h>

volatile int running = 1;

BOOL WINAPI console_handler(DWORD signal) {
    if (signal == CTRL_C_EVENT) {
        running = 0;
        return TRUE;
    }
    return FALSE;
}

int main(int argc, char *argv[]) {
    if (argc != 2) {
        printf("Uso: trainer <trainer_id>\n");
        return 1;
    }
    
    // Configurar handler para Ctrl+C
    SetConsoleCtrlHandler(console_handler, TRUE);
    
    int trainer_id = atoi(argv[1]);
    printf("Treinador %d iniciado (PID: %d)\n", trainer_id, GetCurrentProcessId());
    
    SharedMemory *shm = setup_shared_memory();
    if (!shm) {
        return 1;
    }
    
    // Registrar treinador ativo
    WaitForSingleObject(hMutex, INFINITE);
    shm->trainers_active++;
    ReleaseMutex(hMutex);
    
    typedef struct { const char* nome; const char* tipo; } Species;
    Species species[] = {
        {"Charmander", "Fogo"},
        {"Squirtle",   "Agua"},
        {"Bulbasaur",  "Planta"},
        {"Pikachu",    "Eletrico"},
        {"Abra",       "Psiquico"}
    };
    int speciesCount = (int)(sizeof(species) / sizeof(species[0]));
    
    srand((unsigned int)time(NULL) + trainer_id);
    
    while (running) {
        Sleep(rand() % 3000 + 1000); // 1-4 segundos entre envios
        
        // Verificar shutdown
        if (shm->shutdown) {
            break;
        }
        
        PokemonRequest request;
        request.pokemon_id = rand() % 1000;
    int idx = rand() % speciesCount;
    strncpy(request.nome, species[idx].nome, sizeof(request.nome) - 1);
    request.nome[sizeof(request.nome) - 1] = '\0';
    strncpy(request.tipo, species[idx].tipo, sizeof(request.tipo) - 1);
        request.tipo[sizeof(request.tipo) - 1] = '\0';
        request.nivel = rand() % 50 + 1;
        request.prioridade = (rand() % 2) == 0 ? 1 : 0;
        request.arena_destino = rand() % 3;
        request.timestamp = time(NULL);
        request.processado = 0;
        
        if (produzir_pokemon(shm, request)) {
            printf("Treinador %d enviou %s (Nivel %d) para Arena %d\n", 
                   trainer_id, request.nome, request.nivel, request.arena_destino);
        } else {
            printf("Treinador %d: Buffer cheio, aguardando...\n", trainer_id);
        }
    }
    
    // Desregistrar treinador
    WaitForSingleObject(hMutex, INFINITE);
    shm->trainers_active--;
    ReleaseMutex(hMutex);
    
    printf("Treinador %d finalizado\n", trainer_id);
    cleanup_shared_memory(shm);
    return 0;
}