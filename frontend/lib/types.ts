export interface AnalyzeResponse {
  analysisId: string;
  status: 'processing' | 'complete' | 'failed';
  results: JobAnalysisResult[] | null;
  error: string | null;
  processingDurationMs: number;
}

export interface JobAnalysisResult {
  jobDescriptionIndex: number;
  matchScore: number;
  alignedSkills: string[];
  trueGaps: string[];
  underrepresentedSkills: UnderrepresentedSkill[];
  tailoredResume: TailoredResume;
}

export interface UnderrepresentedSkill {
  skill: string;
  currentText: string;
  reason: string;
}

export interface TailoredResume {
  summary: string | null;
  experienceBullets: TailoredBullet[];
  skillsSection: string | null;
}

export interface TailoredBullet {
  original: string;
  tailored: string;
  role: string | null;
  company: string | null;
}

export type AnalysisStyle = 'metrics-focused' | 'narrative' | 'concise';
