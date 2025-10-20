#ifndef SHARED_MEMORY_H
#define SHARED_MEMORY_H

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#define BUFFER_SIZE 10
#define SHM_NAME "Local\\PokemonExpedition"
#define MUTEX_NAME "Local\\PokemonMutex"
#define SEM_EMPTY_NAME "Local\\PokemonEmpty"
#define SEM_FULL_NAME "Local\\PokemonFull"

typedef struct {
    int pokemon_id;
    char nome[50];
    char tipo[20];
    int nivel;
    int prioridade;
    int arena_destino;
    time_t timestamp;
    int processado;
} PokemonRequest;

typedef struct {
    PokemonRequest requests[BUFFER_SIZE];
    int front;
    int rear;
    int count;
    int arenas_ocupadas;
    int total_batalhas;
    int pokemon_feridos;
    int shutdown;
    int trainers_active;
    int arenas_active;
} SharedMemory;

// Funções de gerenciamento da memória compartilhada
SharedMemory* setup_shared_memory();
void cleanup_shared_memory(SharedMemory* shm);
int produzir_pokemon(SharedMemory *shm, PokemonRequest request);
int consumir_pokemon(SharedMemory *shm, PokemonRequest *request);
HANDLE create_semaphore_with_count(LPCSTR name, int initial_count, int max_count);

// Declarações externas para variáveis globais definidas em shared_memory.c
extern HANDLE hMapFile;
extern HANDLE hMutex;
extern HANDLE hSemEmpty;
extern HANDLE hSemFull;
extern SharedMemory* shm;

#endif