#include "shared_memory.h"

HANDLE hMapFile = NULL;
HANDLE hMutex = NULL;
HANDLE hSemEmpty = NULL;
HANDLE hSemFull = NULL;
SharedMemory* shm = NULL;

HANDLE create_semaphore_with_count(LPCSTR name, int initial_count, int max_count) {
    return CreateSemaphoreA(
        NULL,           // default security attributes
        initial_count,  // initial count
        max_count,      // maximum count
        name            // named semaphore
    );
}

SharedMemory* setup_shared_memory() {
    // Criar mutex
    hMutex = CreateMutexA(NULL, FALSE, MUTEX_NAME);
    if (hMutex == NULL) {
        printf("Erro ao criar mutex: %d\n", GetLastError());
        return NULL;
    }

    // Criar semáforos
    hSemEmpty = create_semaphore_with_count(SEM_EMPTY_NAME, BUFFER_SIZE, BUFFER_SIZE);
    hSemFull = create_semaphore_with_count(SEM_FULL_NAME, 0, BUFFER_SIZE);
    
    if (hSemEmpty == NULL || hSemFull == NULL) {
        printf("Erro ao criar semáforos: %d\n", GetLastError());
        return NULL;
    }

    // Criar memória compartilhada
    hMapFile = CreateFileMappingA(
        INVALID_HANDLE_VALUE,    // use paging file
        NULL,                    // default security
        PAGE_READWRITE,          // read/write access
        0,                       // maximum object size (high-order DWORD)
        sizeof(SharedMemory),    // maximum object size (low-order DWORD)
        SHM_NAME                 // name of mapping object
    );

    if (hMapFile == NULL) {
        printf("Erro ao criar file mapping: %d\n", GetLastError());
        return NULL;
    }

    shm = (SharedMemory*)MapViewOfFile(
        hMapFile,   // handle to map object
        FILE_MAP_ALL_ACCESS, // read/write permission
        0,
        0,
        sizeof(SharedMemory)
    );

    if (shm == NULL) {
        printf("Erro ao mapear view of file: %d\n", GetLastError());
        CloseHandle(hMapFile);
        return NULL;
    }

    // Inicializar se for o primeiro processo
    if (GetLastError() != ERROR_ALREADY_EXISTS) {
        memset(shm, 0, sizeof(SharedMemory));
        shm->front = 0;
        shm->rear = 0;
        shm->count = 0;
        shm->shutdown = 0;
        shm->trainers_active = 0;
        shm->arenas_active = 0;
    }

    return shm;
}

void cleanup_shared_memory(SharedMemory* shm) {
    if (shm) UnmapViewOfFile(shm);
    if (hMapFile) CloseHandle(hMapFile);
    if (hMutex) CloseHandle(hMutex);
    if (hSemEmpty) CloseHandle(hSemEmpty);
    if (hSemFull) CloseHandle(hSemFull);
}

int produzir_pokemon(SharedMemory *shm, PokemonRequest request) {
    DWORD dwWaitResult;
    
    // Esperar por espaço vazio
    dwWaitResult = WaitForSingleObject(hSemEmpty, 5000); // 5 segundos timeout
    if (dwWaitResult != WAIT_OBJECT_0) {
        return 0; // Timeout ou erro
    }
    
    // Esperar pelo mutex
    dwWaitResult = WaitForSingleObject(hMutex, 5000);
    if (dwWaitResult != WAIT_OBJECT_0) {
        ReleaseSemaphore(hSemEmpty, 1, NULL); // Libera o semáforo empty
        return 0;
    }
    
    // Inserir no buffer
    shm->requests[shm->rear] = request;
    shm->rear = (shm->rear + 1) % BUFFER_SIZE;
    shm->count++;
    
    // Liberar mutex e semáforo full
    ReleaseMutex(hMutex);
    ReleaseSemaphore(hSemFull, 1, NULL);
    
    return 1;
}

int consumir_pokemon(SharedMemory *shm, PokemonRequest *request) {
    DWORD dwWaitResult;
    
    // Esperar por item disponível
    dwWaitResult = WaitForSingleObject(hSemFull, 5000);
    if (dwWaitResult != WAIT_OBJECT_0) {
        return 0;
    }
    
    // Esperar pelo mutex
    dwWaitResult = WaitForSingleObject(hMutex, 5000);
    if (dwWaitResult != WAIT_OBJECT_0) {
        ReleaseSemaphore(hSemFull, 1, NULL);
        return 0;
    }
    
    // Remover do buffer
    *request = shm->requests[shm->front];
    shm->front = (shm->front + 1) % BUFFER_SIZE;
    shm->count--;
    
    // Liberar mutex e semáforo empty
    ReleaseMutex(hMutex);
    ReleaseSemaphore(hSemEmpty, 1, NULL);
    
    return 1;
}