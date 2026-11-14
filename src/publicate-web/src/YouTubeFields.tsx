export const youtubeDefaults=JSON.stringify({title:'',privacy:'private'});
export function YouTubeFields({value,onChange}:{value:string;onChange:(value:string)=>void}){
 const settings=JSON.parse(value);
 const change=(patch:Record<string,unknown>)=>onChange(JSON.stringify({...settings,...patch}));
 return <div className="youtube-settings"><p>YouTube Shorts · jeden film pionowy lub kwadratowy do 2:59,99.</p>
 <label>Tytuł filmu<input value={settings.title??''} onChange={e=>change({title:e.target.value})}/></label>
 <small>{Array.from(settings.title??'').length}/100 znaków</small>
 <label>Widoczność<select value={settings.privacy??'private'} onChange={e=>change({privacy:e.target.value})}><option value="private">Prywatny</option><option value="unlisted">Niepubliczny (dostęp przez link)</option><option value="public">Publiczny</option></select></label>
 <label>Czy materiał jest przeznaczony dla dzieci?<select value={typeof settings.madeForKids==='boolean'?String(settings.madeForKids):''} onChange={e=>change({madeForKids:e.target.value===''?null:e.target.value==='true'})}><option value="">Wybierz świadomie</option><option value="false">Nie</option><option value="true">Tak</option></select></label>
 <label>Czy film zawiera realistyczne treści syntetyczne lub zmodyfikowane?<select value={typeof settings.containsSyntheticMedia==='boolean'?String(settings.containsSyntheticMedia):''} onChange={e=>change({containsSyntheticMedia:e.target.value===''?null:e.target.value==='true'})}><option value="">Wybierz świadomie</option><option value="false">Nie</option><option value="true">Tak</option></select></label>
 <small>Dotyczy np. realistycznej sceny, która nie miała miejsca, lub przedstawienia prawdziwej osoby mówiącej coś, czego nie powiedziała.</small>
 <small>Opis z CTA: do 5000 bajtów UTF-8. Zdjęcia i posty tekstowe nie są obsługiwane przez API YouTube.</small></div>;
}
