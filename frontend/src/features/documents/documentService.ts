import { api } from "../../services/api";

export interface DocumentItem {
  id: number;
  originalFileName: string;
  sizeBytes: number;
  status: string;
  chunkCount: number;
  createdAt: string;
}

export const getDocuments = async (): Promise<DocumentItem[]> => {
  const response = await api.get<DocumentItem[]>("/documents");
  return response.data;
};

export const uploadDocument = async (file: File): Promise<DocumentItem> => {
  const formData = new FormData();
  formData.append("file", file);

  const response = await api.post<DocumentItem>("/documents/upload", formData, {
    headers: {
      "Content-Type": "multipart/form-data",
    },
  });

  return response.data;
};
export const deleteDocument = async (documentId: number): Promise<void> => {
  await api.delete(`/documents/${documentId}`);
};