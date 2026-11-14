export const platformOrder=['Facebook','Instagram','TikTok','YouTube','X','LinkedIn'] as const;
export type Platform=typeof platformOrder[number];
export const platformIcons:Record<Platform,string>={Facebook:'f',Instagram:'◎',TikTok:'♪',YouTube:'▶',X:'𝕏',LinkedIn:'in'};
export const platformLabel=(p:Platform)=>p==='YouTube'?'YouTube Shorts':p;
