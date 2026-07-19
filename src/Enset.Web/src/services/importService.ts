import {
  EnsetApiClient,
  type ImportReportResponse,
} from "../api/generated/ensetApiClient";

const apiClient = new EnsetApiClient("");

export const importService = {
  async analyze(
    file: File,
    userId: string,
  ): Promise<ImportReportResponse> {
    return apiClient.analyze(
      userId,
      {
        data: file,
        fileName: file.name,
      },
    );
  },
};