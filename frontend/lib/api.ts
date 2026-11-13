import { AnalyzeResponse } from './types';

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5272';

export async function submitAnalysis(formData: FormData): Promise<{ analysisId: string }> {
  const response = await fetch(`${API_URL}/api/analyze`, {
    method: 'POST',
    body: formData,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to submit analysis: ${response.status} ${errorText}`);
  }

  return response.json();
}

export async function getAnalysis(id: string): Promise<AnalyzeResponse> {
  const response = await fetch(`${API_URL}/api/analyze/${id}`, {
    method: 'GET',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to fetch analysis: ${response.status} ${errorText}`);
  }

  return response.json();
}

export async function deleteAnalysis(id: string): Promise<void> {
  const response = await fetch(`${API_URL}/api/analyze/${id}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to delete analysis: ${response.status} ${errorText}`);
  }
}

export async function getTailoredResume(id: string, index: number): Promise<Blob> {
  const response = await fetch(`${API_URL}/api/analyze/${id}/resume/${index}`);

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`Failed to fetch tailored resume: ${response.status} ${errorText}`);
  }

  return response.blob();
}
