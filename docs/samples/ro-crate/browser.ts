import type { DataHubClient } from "@nfdi4plants/datahub-client";

/** Download an ARC's JSON-LD metadata using the browser's save-file flow. */
export async function downloadROCrate(client: DataHubClient, projectId: number): Promise<void> {
  const metadata = await client.Packages.DownloadROCrateAsync(projectId);
  const blob = new Blob([metadata], { type: "application/ld+json;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = "arc-ro-crate-metadata.json";
  link.hidden = true;
  document.body.append(link);
  try {
    link.click();
  } finally {
    link.remove();
    // Give the browser time to start the download before releasing its URL.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}
