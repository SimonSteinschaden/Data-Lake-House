export interface ApiProblem {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ApiProblem;

  constructor(status: number, message: string, problem?: ApiProblem) {
    super(message);

    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}