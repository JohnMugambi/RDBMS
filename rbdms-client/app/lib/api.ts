/* eslint-disable @typescript-eslint/no-explicit-any */
// Aspire injects this from the AppHost configuration above.
const rawBaseUrl = process.env.NEXT_PUBLIC_API_URL;

// Ensure we have a valid URL and append /api
const API_BASE_URL = rawBaseUrl 
  ? `${rawBaseUrl.replace(/\/$/, '')}/api` 
  : 'https://localhost:7218/api'; // This fallback is now only for running without Aspire

export interface Task {
  Id: number;
  Title: string;
  Description?: string;
  Completed: boolean;
  Priority?: 'Low' | 'Medium' | 'High';
  CreatedAt: string;
}

export interface ApiResponse<T> {
  Success: boolean;
  Message?: string;
  Data?: T;
  RowsAffected?: number;
  Error?: string;
}

export interface SqlQueryRequest {
  sql: string;
}

export interface SqlQueryResult {
  data: any[];
  columns: string[];
}

class ApiError extends Error {
  constructor(public statusCode: number, message: string) {
    super(message);
    this.name = 'ApiError';
  }
}

async function fetchApi<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<ApiResponse<T>> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  const config: RequestInit = {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  };

  try {
    const response = await fetch(url, config);
    
    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      throw new ApiError(
        response.status,
        errorData.error || `HTTP ${response.status}: ${response.statusText}`
      );
    }

    return await response.json();
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    throw new ApiError(0, `Network error: ${(error as Error).message}`);
  }
}

// Task API endpoints
export const tasksApi = {
  // Get all tasks
  async getAll(): Promise<Task[]> {
    const response = await fetchApi<Task[]>('/tasks');
    return response.Data || [];
  },

  // Get single task by ID
  async getById(id: number): Promise<Task> {
    const response = await fetchApi<Task>(`/tasks/${id}`);
    if (!response.Data) {
      throw new ApiError(404, 'Task not found');
    }
    return response.Data;
  },

  // Create new task
  async create(task: Omit<Task, 'Id'>): Promise<Task> {
    // Generate ID based on timestamp
    const Id = 0;
    const newTask = { ...task, Id };
    
    const response = await fetchApi<Task>('/tasks', {
      method: 'POST',
      body: JSON.stringify(newTask),
    });
    
    if (!response.Success) {
      throw new ApiError(400, response.Error || 'Failed to create task');
    }
    
    return response.Data || newTask;
  },

  // Update existing task
  async update(id: number, updates: Partial<Omit<Task, 'id' | 'createdAt'>>): Promise<void> {
    const response = await fetchApi<any>(`/tasks/${id}`, {
      method: 'PUT',
      body: JSON.stringify(updates),
    });
    
    if (!response.Success) {
      throw new ApiError(400, response.Error || 'Failed to update task');
    }
  },

  // Delete task
  async delete(id: number): Promise<void> {
    const response = await fetchApi<any>(`/tasks/${id}`, {
      method: 'DELETE',
    });
    
    if (!response.Success) {
      throw new ApiError(400, response.Error || 'Failed to delete task');
    }
  },
};

// Database API endpoints
export const databaseApi = {
  // Execute custom SQL query
  async executeSql(sql: string): Promise<SqlQueryResult> {
    const response = await fetchApi<{ data: SqlQueryResult }>('/execute', {
      method: 'POST',
      body: JSON.stringify({ sql } as SqlQueryRequest),
    });
    
    if (!response.Success || !response.Data) {
      throw new ApiError(400, response.Error || 'Failed to execute SQL');
    }
    
    return response.Data.data;
  },

  // Get list of all tables
  async getTables(): Promise<string[]> {
    const response = await fetchApi<{ tables: string[] }>('/tables');
    return response.Data?.tables || [];
  },

  // Get schema for a specific table
  async getTableSchema(tableName: string): Promise<any> {
    const response = await fetchApi<any>(`/tables/${tableName}/schema`);
    return response.Data;
  },
};

export { ApiError };