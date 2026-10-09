export interface IRespuesta<T> {
    success: boolean;
    data: T | null;
    error: string;
}
