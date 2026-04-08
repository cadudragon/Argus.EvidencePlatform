delete from argus.evidence_blobs
using argus.evidence_items
where argus.evidence_blobs."EvidenceItemId" = argus.evidence_items."Id"
  and argus.evidence_items."EvidenceType" = 'Image';

delete from argus.evidence_items
where "EvidenceType" = 'Image';
